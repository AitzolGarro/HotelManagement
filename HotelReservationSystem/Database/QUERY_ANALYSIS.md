# Análisis de Rendimiento de Consultas - Sistema de Gestión de Reservas Hoteleras

**Fecha de análisis:** 2025  
**Entorno:** Desarrollo (SQLite) / Producción (SQL Server)  
**Metodología:** Revisión estática de código en repositorios EF Core + análisis de patrones de consulta

---

## Resumen Ejecutivo

Se identificaron **15 patrones de consulta lentos** distribuidos en 4 repositorios principales. Los problemas más críticos son:

1. Carga de entidades completas sin proyección en consultas de lista
2. Problema N+1 en `IsRoomAvailableAsync` (carga todas las reservas en memoria)
3. Búsquedas de texto con `ToLower().Contains()` que impiden uso de índices
4. Consultas sin `AsNoTracking()` en operaciones de solo lectura
5. Ausencia de índices en columnas de búsqueda frecuente (`GuestId`, `CreatedAt`, `DocumentNumber`)

---

## Top 20 Patrones de Consulta Lentos Identificados


### CONSULTA #1 — `RoomRepository.IsRoomAvailableAsync` ⚠️ CRÍTICO

**Archivo:** `Data/Repositories/RoomRepository.cs`  
**Problema:** Patrón N+1 — carga toda la colección `Reservations` de la habitación en memoria y filtra en C#

```csharp
// CÓDIGO ACTUAL (problemático)
var room = await _dbSet
    .Include(r => r.Reservations)   // Carga TODAS las reservas de la habitación
    .FirstOrDefaultAsync(r => r.Id == roomId);

// Filtrado en memoria (no en base de datos)
var conflictingReservations = room.Reservations
    .Where(r => r.Status != ReservationStatus.Cancelled && ...);
```

**Impacto estimado en producción:** >500ms para habitaciones con historial extenso  
**Índice faltante:** Ninguno — el problema es arquitectural (filtrado en memoria)  
**Recomendación:** Reemplazar con consulta directa a `Reservations` con filtros en SQL

```sql
-- Consulta equivalente optimizada
SELECT COUNT(1) FROM Reservations
WHERE RoomId = @roomId
  AND Status NOT IN (3, 6)  -- No Cancelled/CheckedOut
  AND CheckInDate < @checkOut
  AND CheckOutDate > @checkIn
  AND (@excludeId IS NULL OR Id != @excludeId)
```

---

### CONSULTA #2 — `GuestRepository.SearchGuestsAsync` ⚠️ CRÍTICO

**Archivo:** `Data/Repositories/GuestRepository.cs`  
**Problema:** Búsqueda con `ToLower().Contains()` genera `LIKE '%term%'` — no puede usar índices, hace full table scan

```csharp
// CÓDIGO ACTUAL (problemático)
var lowerSearchTerm = searchTerm.ToLower();
return await _dbSet.AsNoTracking()
    .Where(g => g.FirstName.ToLower().Contains(lowerSearchTerm) ||
               g.LastName.ToLower().Contains(lowerSearchTerm) ||
               (g.Email != null && g.Email.ToLower().Contains(lowerSearchTerm)) || ...)
    .ToListAsync();
```

**Impacto estimado en producción:** >300ms con 10,000+ huéspedes  
**Índice faltante:** `IX_Guests_Name (LastName, FirstName)`, `IX_Guests_Email (Email)`  
**Sin límite de resultados:** Puede retornar miles de registros  
**Recomendación:** Agregar paginación + índice de texto completo (SQL Server Full-Text Search)

```sql
-- Índice recomendado
CREATE INDEX IX_Guests_Name ON Guests (LastName, FirstName) INCLUDE (Email, Phone);
CREATE INDEX IX_Guests_DocumentNumber ON Guests (DocumentNumber);
-- Para SQL Server: considerar Full-Text Index en FirstName, LastName, Email
```

---

### CONSULTA #3 — `ReservationRepository.SearchReservationsAsync` ⚠️ ALTO

**Archivo:** `Data/Repositories/ReservationRepository.cs`  
**Problema:** Búsqueda de nombre de huésped con `ToLower().Contains()` en join — full table scan en Guests + Reservations

```csharp
// CÓDIGO ACTUAL (problemático)
if (!string.IsNullOrEmpty(criteria.GuestName))
{
    var term = criteria.GuestName.ToLower();
    query = query.Where(r => r.Guest != null &&
        (r.Guest.FirstName.ToLower().Contains(term) ||
         r.Guest.LastName.ToLower().Contains(term)));
}
```

**Impacto estimado en producción:** >400ms — requiere join + full scan en tabla Guests  
**Índice faltante:** `IX_Guests_Name` (ya identificado arriba)  
**Recomendación:** Usar `EF.Functions.Like` o búsqueda por prefijo en lugar de `Contains`

---

### CONSULTA #4 — `ReservationRepository.SearchReservationsAsync` — BookingReference ⚠️ ALTO

**Archivo:** `Data/Repositories/ReservationRepository.cs`  
**Problema:** Búsqueda de referencia con `ToLower().Contains()` — impide uso del índice existente en `BookingReference`

```csharp
// CÓDIGO ACTUAL (problemático)
if (!string.IsNullOrEmpty(criteria.BookingReference))
{
    var term = criteria.BookingReference.ToLower();
    query = query.Where(r => r.BookingReference != null &&
        r.BookingReference.ToLower().Contains(term));  // LIKE '%term%' — no usa índice
}
```

**Impacto estimado en producción:** >200ms — el índice `IX_Reservations_BookingReference` existe pero no se usa  
**Recomendación:** Cambiar a búsqueda exacta o por prefijo: `r.BookingReference == criteria.BookingReference`

---

### CONSULTA #5 — `ReservationRepository.GetReservationsByStatusAsync` ⚠️ ALTO

**Archivo:** `Data/Repositories/ReservationRepository.cs`  
**Problema:** Sin paginación — puede retornar miles de reservas con todos sus relacionados

```csharp
// CÓDIGO ACTUAL (problemático)
public async Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(
    ReservationStatus status, int? hotelId = null)
{
    // Sin .Take() ni paginación — retorna TODOS los registros
    return await query
        .Include(r => r.Hotel)
        .Include(r => r.Room)
        .Include(r => r.Guest)
        .OrderBy(r => r.CheckInDate)
        .ToListAsync();
}
```

**Impacto estimado en producción:** >1000ms con 50,000+ reservas confirmadas  
**Recomendación:** Agregar paginación obligatoria o límite máximo de resultados

---

### CONSULTA #6 — `ReservationRepository.GetReservationsByDateRangeAsync` ⚠️ ALTO

**Archivo:** `Data/Repositories/ReservationRepository.cs`  
**Problema:** Sin paginación para rangos de fecha amplios; carga entidades completas con 3 Includes

```csharp
// CÓDIGO ACTUAL — sin límite de resultados
return await query
    .Include(r => r.Hotel)
    .Include(r => r.Room)
    .Include(r => r.Guest)
    .OrderBy(r => r.CheckInDate)
    .ThenBy(r => r.Room.RoomNumber)  // Ordenamiento por navegación — puede ser lento
    .ToListAsync();
```

**Impacto estimado en producción:** >800ms para rangos de 1 año con hotel ocupado  
**Índice faltante:** El índice `IX_Reservations_DateRange_HotelId` en `OptimizationIndexes.sql` cubre este caso  
**Recomendación:** Agregar paginación + proyección a DTO en lugar de cargar entidades completas

---

### CONSULTA #7 — `RoomRepository.GetAvailableRoomsAsync` ⚠️ ALTO

**Archivo:** `Data/Repositories/RoomRepository.cs`  
**Problema:** Subconsulta correlacionada con `!r.Reservations.Any(...)` — puede generar plan de ejecución ineficiente

```csharp
// CÓDIGO ACTUAL
return await _dbSet
    .Where(r => r.HotelId == hotelId &&
               r.Status == RoomStatus.Available &&
               !r.Reservations.Any(res =>
                   res.Status != ReservationStatus.Cancelled &&
                   res.CheckInDate < checkOut &&
                   res.CheckOutDate > checkIn))
    .ToListAsync();
```

**Impacto estimado en producción:** >300ms — subconsulta NOT EXISTS por cada habitación  
**Índice faltante:** `IX_Reservations_Availability_Check` en `OptimizationIndexes.sql` cubre este caso  
**Recomendación:** El índice filtrado `IX_Reservations_Availability_Check` debe estar presente; verificar que EF Core lo use

---

### CONSULTA #8 — `RoomRepository.GetRoomWithReservationsAsync` ⚠️ MEDIO

**Archivo:** `Data/Repositories/RoomRepository.cs`  
**Problema:** Carga todas las reservas de la habitación sin filtro de fecha cuando `fromDate`/`toDate` son null; el filtro de fecha en el `Where` de la habitación no filtra las reservas incluidas

```csharp
// CÓDIGO ACTUAL — el filtro de fecha no limita las reservas cargadas
var query = _dbSet
    .Include(r => r.Reservations)
        .ThenInclude(res => res.Guest)
    .Where(r => r.Id == roomId);

// Este Where filtra habitaciones, no las reservas incluidas
if (fromDate.HasValue && toDate.HasValue)
    query = query.Where(r => r.Reservations.Any(res => ...));
```

**Impacto estimado en producción:** >400ms — carga historial completo de reservas de la habitación  
**Recomendación:** Usar `AsSplitQuery()` o filtrar reservas con `.Where()` en la colección incluida

---

### CONSULTA #9 — `HotelRepository.GetHotelWithReservationsAsync` ⚠️ MEDIO

**Archivo:** `Data/Repositories/HotelRepository.cs`  
**Problema:** Carga todas las reservas del hotel con dos niveles de ThenInclude — puede ser muy costoso

```csharp
// CÓDIGO ACTUAL — carga masiva de datos
return await _dbSet
    .Include(h => h.Reservations)
        .ThenInclude(r => r.Room)
    .Include(h => h.Reservations)
        .ThenInclude(r => r.Guest)
    .Where(h => h.Id == hotelId)
    .FirstOrDefaultAsync();
```

**Impacto estimado en producción:** >2000ms para hotel con 10,000+ reservas históricas  
**Recomendación:** Agregar filtro de fecha obligatorio + paginación de reservas; considerar consulta separada

---

### CONSULTA #10 — `GuestRepository.GetGuestWithReservationsAsync` ⚠️ MEDIO

**Archivo:** `Data/Repositories/GuestRepository.cs`  
**Problema:** Sin `AsNoTracking()` en consulta de solo lectura; carga historial completo de reservas

```csharp
// CÓDIGO ACTUAL — sin AsNoTracking, sin límite
return await _dbSet
    .Include(g => g.Reservations)
        .ThenInclude(r => r.Hotel)
    .Include(g => g.Reservations)
        .ThenInclude(r => r.Room)
    .FirstOrDefaultAsync(g => g.Id == guestId);
```

**Impacto estimado en producción:** >300ms para huéspedes frecuentes con 100+ reservas  
**Recomendación:** Agregar `AsNoTracking()` + limitar reservas a las más recientes (ej. últimas 20)

---

### CONSULTA #11 — `ReservationRepository.GetCheckInsForDateAsync` ⚠️ MEDIO

**Archivo:** `Data/Repositories/ReservationRepository.cs`  
**Problema:** Sin `AsNoTracking()` en consulta de solo lectura del dashboard

```csharp
// CÓDIGO ACTUAL — sin AsNoTracking
var query = _dbSet
    .Include(r => r.Hotel)
    .Include(r => r.Room)
    .Include(r => r.Guest)
    .Where(r => r.CheckInDate.Date == date.Date && ...);
```

**Impacto estimado en producción:** +20-30% overhead de tracking innecesario  
**Índice faltante:** `IX_Reservations_Today_CheckIn` en `OptimizationIndexes.sql` cubre este caso  
**Recomendación:** Agregar `AsNoTracking()` — es consulta de solo lectura para el dashboard

---

### CONSULTA #12 — `ReservationRepository.GetCheckOutsForDateAsync` ⚠️ MEDIO

**Archivo:** `Data/Repositories/ReservationRepository.cs`  
**Problema:** Mismo problema que #11 — sin `AsNoTracking()` en consulta de solo lectura

**Recomendación:** Agregar `AsNoTracking()` — misma corrección que #11

---

### CONSULTA #13 — `Repository<T>.GetAllAsync` ⚠️ MEDIO

**Archivo:** `Data/Repositories/Repository.cs`  
**Problema:** Método genérico sin paginación ni filtros — retorna TODA la tabla

```csharp
// CÓDIGO ACTUAL — peligroso en tablas grandes
public virtual async Task<IEnumerable<T>> GetAllAsync()
{
    return await _dbSet.ToListAsync();  // Sin límite, sin AsNoTracking
}
```

**Impacto estimado en producción:** Potencialmente ilimitado — depende del tamaño de la tabla  
**Recomendación:** Agregar `AsNoTracking()` como mínimo; deprecar en favor de métodos paginados

---

### CONSULTA #14 — `GuestRepository.GetPagedGuestsAsync` ⚠️ BAJO

**Archivo:** `Data/Repositories/GuestRepository.cs`  
**Problema:** El `CountAsync()` se ejecuta sobre toda la tabla sin filtros — costoso en tablas grandes

```csharp
// CÓDIGO ACTUAL — COUNT(*) sin filtros
var query = _dbSet.AsNoTracking();
var totalCount = await query.CountAsync();  // Cuenta TODOS los huéspedes
```

**Impacto estimado en producción:** >100ms con 100,000+ huéspedes  
**Recomendación:** Considerar caché del conteo total o usar `COUNT` aproximado para paginación

---

### CONSULTA #15 — `RoomRepository.GetAllRoomsWithHotelAsync` ⚠️ BAJO

**Archivo:** `Data/Repositories/RoomRepository.cs`  
**Problema:** Sin `AsNoTracking()` y sin paginación — carga todas las habitaciones de todos los hoteles

```csharp
// CÓDIGO ACTUAL — sin AsNoTracking, sin paginación
return await _dbSet
    .Include(r => r.Hotel)
    .OrderBy(r => r.Hotel.Name)
    .ThenBy(r => r.RoomNumber)
    .ToListAsync();
```

**Impacto estimado en producción:** >200ms con 500+ habitaciones en múltiples hoteles  
**Recomendación:** Agregar `AsNoTracking()` + paginación

---

## Resumen de Índices Faltantes

| Tabla | Columna(s) | Tipo | Consulta afectada |
|-------|-----------|------|-------------------|
| `Guests` | `(LastName, FirstName)` | NONCLUSTERED | #2, #3 |
| `Guests` | `DocumentNumber` | NONCLUSTERED | #2 |
| `Reservations` | `(GuestId, CheckInDate DESC)` | NONCLUSTERED | #5, #6 |
| `Reservations` | `CreatedAt DESC` | NONCLUSTERED | Paginación por fecha de creación |
| `Reservations` | `(CheckInDate, HotelId)` filtrado por Status | FILTERED | #11, #12 |

> **Nota:** Los índices en `OptimizationIndexes.sql` ya cubren los patrones de `RoomId+Fechas`, `HotelId+Status`, `BookingReference` y `HotelId+CheckInDate+CheckOutDate`. Los índices de la tabla anterior son **adicionales** a los ya existentes.

---

## Índices ya Definidos en el Contexto EF Core

Los siguientes índices están configurados en `HotelReservationContext.cs` y se crean automáticamente con las migraciones:

| Tabla | Índice EF Core | Estado |
|-------|---------------|--------|
| `Rooms` | `(HotelId, RoomNumber)` UNIQUE | ✅ Existe |
| `Rooms` | `(HotelId, Status)` | ✅ Existe |
| `Guests` | `Email` | ✅ Existe |
| `Reservations` | `(CheckInDate, CheckOutDate, Status)` | ✅ Existe |
| `Reservations` | `(HotelId, Status)` | ✅ Existe |
| `Reservations` | `(RoomId, CheckInDate, CheckOutDate)` | ✅ Existe |
| `Reservations` | `BookingReference` | ✅ Existe |
| `UserHotelAccess` | `(UserId, HotelId)` UNIQUE | ✅ Existe |
| `Invoices` | `InvoiceNumber` UNIQUE | ✅ Existe |

---

## Recomendaciones Prioritarias

### Prioridad 1 — Correcciones Arquitecturales (sin cambio de esquema)

1. **`RoomRepository.IsRoomAvailableAsync`** — Reemplazar carga en memoria con consulta directa a `Reservations`
2. **`GuestRepository.SearchGuestsAsync`** — Agregar paginación + límite de resultados (máx. 50)
3. **`ReservationRepository.GetReservationsByStatusAsync`** — Agregar paginación obligatoria
4. **Todas las consultas de solo lectura** — Agregar `AsNoTracking()` donde falta (#10, #11, #12, #15)

### Prioridad 2 — Nuevos Índices de Base de Datos

```sql
-- Índice para búsqueda de huéspedes por nombre
CREATE INDEX IX_Guests_Name ON Guests (LastName, FirstName) INCLUDE (Email, Phone);

-- Índice para búsqueda por número de documento
CREATE INDEX IX_Guests_DocumentNumber ON Guests (DocumentNumber);

-- Índice para paginación de reservas por fecha de creación
CREATE INDEX IX_Reservations_CreatedAt ON Reservations (CreatedAt DESC) INCLUDE (HotelId, Status);

-- Índice para historial de reservas por huésped
CREATE INDEX IX_Reservations_GuestId_CheckIn
ON Reservations (GuestId, CheckInDate DESC)
INCLUDE (HotelId, RoomId, Status, TotalAmount);
```

### Prioridad 3 — Proyecciones y Optimizaciones de Consulta

1. Implementar DTOs de proyección para consultas de lista (evitar cargar entidades completas)
2. Usar `AsSplitQuery()` en consultas con múltiples `Include` de colecciones
3. Implementar caché de segundo nivel para datos de hoteles y habitaciones (5 min TTL)
4. Considerar Full-Text Search de SQL Server para búsquedas de texto en `Guests`

---

## Script de Análisis SQL (SQL Server Query Store)

Ver archivo `QUERY_ANALYSIS.sql` en esta misma carpeta para los scripts de análisis de Query Store y DMVs de SQL Server.

---

*Análisis generado mediante revisión estática de código. En producción con SQL Server, ejecutar los scripts de Query Store para obtener tiempos de ejecución reales y planes de consulta.*
