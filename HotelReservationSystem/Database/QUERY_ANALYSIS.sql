-- =============================================================================
-- ANÁLISIS DE RENDIMIENTO DE CONSULTAS - Sistema de Gestión de Reservas
-- Ejecutar en SQL Server (producción) para identificar consultas lentas
-- Requiere: Query Store habilitado en la base de datos
-- =============================================================================

-- -----------------------------------------------------------------------------
-- SECCIÓN 1: Habilitar Query Store (si no está habilitado)
-- -----------------------------------------------------------------------------
-- ALTER DATABASE HotelReservationDB SET QUERY_STORE = ON;
-- ALTER DATABASE HotelReservationDB SET QUERY_STORE (
--     OPERATION_MODE = READ_WRITE,
--     CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30),
--     DATA_FLUSH_INTERVAL_SECONDS = 900,
--     INTERVAL_LENGTH_MINUTES = 60,
--     MAX_STORAGE_SIZE_MB = 1000,
--     QUERY_CAPTURE_MODE = AUTO,
--     SIZE_BASED_CLEANUP_MODE = AUTO
-- );

-- -----------------------------------------------------------------------------
-- SECCIÓN 2: Top 20 consultas más lentas (Query Store)
-- Identifica consultas con tiempo de ejecución promedio > 100ms
-- -----------------------------------------------------------------------------
SELECT TOP 20
    qsq.query_id,
    qsqt.query_sql_text,
    qsrs.avg_duration / 1000.0                  AS avg_duration_ms,
    qsrs.max_duration / 1000.0                  AS max_duration_ms,
    qsrs.min_duration / 1000.0                  AS min_duration_ms,
    qsrs.count_executions,
    qsrs.avg_logical_io_reads,
    qsrs.avg_physical_io_reads,
    qsrs.avg_cpu_time / 1000.0                  AS avg_cpu_ms,
    qsrs.avg_rowcount,
    qsp.query_plan
FROM sys.query_store_query          AS qsq
JOIN sys.query_store_query_text     AS qsqt ON qsq.query_text_id = qsqt.query_text_id
JOIN sys.query_store_plan           AS qsp  ON qsq.query_id = qsp.query_id
JOIN sys.query_store_runtime_stats  AS qsrs ON qsp.plan_id = qsrs.plan_id
WHERE qsrs.avg_duration > 100000   -- > 100ms (en microsegundos)
  AND qsrs.count_executions > 5    -- Ejecutada al menos 5 veces
ORDER BY qsrs.avg_duration DESC;

-- -----------------------------------------------------------------------------
-- SECCIÓN 3: Consultas con mayor consumo de I/O (candidatas a índices)
-- -----------------------------------------------------------------------------
SELECT TOP 20
    qsq.query_id,
    qsqt.query_sql_text,
    qsrs.avg_logical_io_reads                   AS avg_logical_reads,
    qsrs.avg_physical_io_reads                  AS avg_physical_reads,
    qsrs.avg_duration / 1000.0                  AS avg_duration_ms,
    qsrs.count_executions,
    qsrs.avg_rowcount
FROM sys.query_store_query          AS qsq
JOIN sys.query_store_query_text     AS qsqt ON qsq.query_text_id = qsqt.query_text_id
JOIN sys.query_store_plan           AS qsp  ON qsq.query_id = qsp.query_id
JOIN sys.query_store_runtime_stats  AS qsrs ON qsp.plan_id = qsrs.plan_id
WHERE qsrs.count_executions > 5
ORDER BY qsrs.avg_logical_io_reads DESC;

-- -----------------------------------------------------------------------------
-- SECCIÓN 4: Índices faltantes sugeridos por el optimizador de SQL Server
-- -----------------------------------------------------------------------------
SELECT TOP 20
    ROUND(migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans), 0)
                                                AS improvement_measure,
    'CREATE INDEX IX_' + OBJECT_NAME(mid.object_id) + '_'
        + REPLACE(REPLACE(REPLACE(ISNULL(mid.equality_columns, ''), '[', ''), ']', ''), ', ', '_')
        + CASE WHEN mid.inequality_columns IS NOT NULL
               THEN '_' + REPLACE(REPLACE(REPLACE(mid.inequality_columns, '[', ''), ']', ''), ', ', '_')
               ELSE '' END
        + ' ON ' + mid.statement
        + ' (' + ISNULL(mid.equality_columns, '')
        + CASE WHEN mid.equality_columns IS NOT NULL AND mid.inequality_columns IS NOT NULL THEN ', ' ELSE '' END
        + ISNULL(mid.inequality_columns, '')
        + ')'
        + ISNULL(' INCLUDE (' + mid.included_columns + ')', '')
                                                AS create_index_statement,
    mid.statement                               AS table_name,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns,
    migs.user_seeks,
    migs.user_scans,
    migs.avg_total_user_cost,
    migs.avg_user_impact
FROM sys.dm_db_missing_index_groups             AS mig
JOIN sys.dm_db_missing_index_group_stats        AS migs ON mig.index_group_handle = migs.group_handle
JOIN sys.dm_db_missing_index_details            AS mid  ON mig.index_handle = mid.index_handle
WHERE mid.database_id = DB_ID()
ORDER BY improvement_measure DESC;

-- -----------------------------------------------------------------------------
-- SECCIÓN 5: Índices existentes con bajo uso (candidatos a eliminación)
-- -----------------------------------------------------------------------------
SELECT
    OBJECT_NAME(i.object_id)                    AS table_name,
    i.name                                      AS index_name,
    i.type_desc,
    ius.user_seeks,
    ius.user_scans,
    ius.user_lookups,
    ius.user_updates,
    ius.last_user_seek,
    ius.last_user_scan
FROM sys.indexes                                AS i
LEFT JOIN sys.dm_db_index_usage_stats           AS ius
    ON i.object_id = ius.object_id
    AND i.index_id = ius.index_id
    AND ius.database_id = DB_ID()
WHERE OBJECT_NAME(i.object_id) IN ('Reservations', 'Rooms', 'Hotels', 'Guests')
  AND i.type > 0  -- Excluir heap
ORDER BY OBJECT_NAME(i.object_id), ISNULL(ius.user_seeks, 0) + ISNULL(ius.user_scans, 0) ASC;

-- -----------------------------------------------------------------------------
-- SECCIÓN 6: Análisis de fragmentación de índices
-- -----------------------------------------------------------------------------
SELECT
    OBJECT_NAME(ips.object_id)                  AS table_name,
    i.name                                      AS index_name,
    ips.index_type_desc,
    ips.avg_fragmentation_in_percent,
    ips.page_count,
    CASE
        WHEN ips.avg_fragmentation_in_percent > 30 THEN 'REBUILD'
        WHEN ips.avg_fragmentation_in_percent > 10 THEN 'REORGANIZE'
        ELSE 'OK'
    END                                         AS action_needed
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') AS ips
JOIN sys.indexes                                AS i
    ON ips.object_id = i.object_id
    AND ips.index_id = i.index_id
WHERE OBJECT_NAME(ips.object_id) IN ('Reservations', 'Rooms', 'Hotels', 'Guests')
  AND ips.page_count > 100
ORDER BY ips.avg_fragmentation_in_percent DESC;

-- -----------------------------------------------------------------------------
-- SECCIÓN 7: Simulación de consultas lentas identificadas en el código
-- Ejecutar con SET STATISTICS IO ON; SET STATISTICS TIME ON; para medir
-- -----------------------------------------------------------------------------

-- Consulta #1: Verificación de disponibilidad (patrón actual — problemático)
-- Equivalente a RoomRepository.IsRoomAvailableAsync con carga en memoria
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Patrón ACTUAL (ineficiente): carga todas las reservas de la habitación
SELECT r.Id, r.RoomNumber, r.Status,
       res.Id AS ReservationId, res.CheckInDate, res.CheckOutDate, res.Status AS ResStatus
FROM Rooms r
LEFT JOIN Reservations res ON res.RoomId = r.Id
WHERE r.Id = 1;  -- Carga TODAS las reservas para filtrar en memoria

-- Patrón OPTIMIZADO: consulta directa con filtros en SQL
SELECT COUNT(1) AS ConflictCount
FROM Reservations
WHERE RoomId = 1
  AND Status NOT IN (3, 6)  -- No Cancelled, No CheckedOut
  AND CheckInDate < '2025-12-31'
  AND CheckOutDate > '2025-12-01';

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;

-- -----------------------------------------------------------------------------
-- Consulta #2: Búsqueda de huéspedes (patrón actual — full table scan)
-- Equivalente a GuestRepository.SearchGuestsAsync
-- -----------------------------------------------------------------------------
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Patrón ACTUAL (ineficiente): LIKE '%term%' — no usa índices
SELECT Id, FirstName, LastName, Email, Phone
FROM Guests
WHERE LOWER(FirstName) LIKE '%garcia%'
   OR LOWER(LastName) LIKE '%garcia%'
   OR LOWER(Email) LIKE '%garcia%';

-- Patrón OPTIMIZADO: búsqueda por prefijo — puede usar índice
SELECT Id, FirstName, LastName, Email, Phone
FROM Guests
WHERE LastName LIKE 'garcia%'  -- Búsqueda por prefijo usa índice
   OR FirstName LIKE 'garcia%'
ORDER BY LastName, FirstName
OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY;  -- Paginación obligatoria

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;

-- -----------------------------------------------------------------------------
-- Consulta #3: Reservas por estado sin paginación (patrón actual — sin límite)
-- Equivalente a ReservationRepository.GetReservationsByStatusAsync
-- -----------------------------------------------------------------------------
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Patrón ACTUAL (ineficiente): sin paginación
SELECT r.*, h.Name AS HotelName, rm.RoomNumber, g.FirstName, g.LastName
FROM Reservations r
JOIN Hotels h ON r.HotelId = h.Id
JOIN Rooms rm ON r.RoomId = rm.Id
JOIN Guests g ON r.GuestId = g.Id
WHERE r.Status = 2  -- Confirmed
ORDER BY r.CheckInDate;

-- Patrón OPTIMIZADO: con paginación
SELECT r.Id, r.BookingReference, r.CheckInDate, r.CheckOutDate,
       r.Status, r.TotalAmount,
       h.Name AS HotelName, rm.RoomNumber,
       g.FirstName + ' ' + g.LastName AS GuestName
FROM Reservations r
JOIN Hotels h ON r.HotelId = h.Id
JOIN Rooms rm ON r.RoomId = rm.Id
JOIN Guests g ON r.GuestId = g.Id
WHERE r.Status = 2
ORDER BY r.CheckInDate
OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY;

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;

-- -----------------------------------------------------------------------------
-- SECCIÓN 8: Índices adicionales recomendados (complementan OptimizationIndexes.sql)
-- -----------------------------------------------------------------------------

-- Índice para búsqueda de huéspedes por nombre (complementa IX_Guests_Email existente)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Guests_Name')
CREATE NONCLUSTERED INDEX IX_Guests_Name
ON Guests (LastName, FirstName)
INCLUDE (Email, Phone, DocumentNumber);

-- Índice para búsqueda por número de documento
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Guests_DocumentNumber')
CREATE NONCLUSTERED INDEX IX_Guests_DocumentNumber
ON Guests (DocumentNumber)
WHERE DocumentNumber IS NOT NULL;

-- Índice para paginación de reservas por fecha de creación
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_CreatedAt')
CREATE NONCLUSTERED INDEX IX_Reservations_CreatedAt
ON Reservations (CreatedAt DESC)
INCLUDE (HotelId, RoomId, GuestId, Status, TotalAmount);

-- Índice para historial de reservas por huésped (complementa índices existentes)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_GuestId_CheckIn')
CREATE NONCLUSTERED INDEX IX_Reservations_GuestId_CheckIn
ON Reservations (GuestId, CheckInDate DESC)
INCLUDE (HotelId, RoomId, Status, TotalAmount, BookingReference);

PRINT 'Análisis de consultas completado. Revisar resultados de cada sección.';
