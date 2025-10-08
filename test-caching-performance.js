// Test script for caching and performance optimization
const baseUrl = 'https://localhost:7001/api';

// Test performance monitoring endpoints
async function testPerformanceMonitoring() {
    console.log('Testing Performance Monitoring...');
    
    try {
        // Test health endpoint
        const healthResponse = await fetch(`${baseUrl}/performance/health`, {
            headers: {
                'Authorization': 'Bearer YOUR_JWT_TOKEN_HERE'
            }
        });
        
        if (healthResponse.ok) {
            const healthData = await healthResponse.json();
            console.log('✅ Health Status:', healthData);
        } else {
            console.log('❌ Health endpoint failed:', healthResponse.status);
        }
        
        // Test metrics endpoint
        const metricsResponse = await fetch(`${baseUrl}/performance/metrics`, {
            headers: {
                'Authorization': 'Bearer YOUR_JWT_TOKEN_HERE'
            }
        });
        
        if (metricsResponse.ok) {
            const metricsData = await metricsResponse.json();
            console.log('✅ Performance Metrics:', metricsData);
        } else {
            console.log('❌ Metrics endpoint failed:', metricsResponse.status);
        }
        
    } catch (error) {
        console.error('❌ Performance monitoring test failed:', error);
    }
}

// Test static data caching
async function testStaticDataCaching() {
    console.log('Testing Static Data Caching...');
    
    try {
        // Test room types endpoint
        const roomTypesResponse = await fetch(`${baseUrl}/staticdata/room-types`, {
            headers: {
                'Authorization': 'Bearer YOUR_JWT_TOKEN_HERE'
            }
        });
        
        if (roomTypesResponse.ok) {
            const roomTypes = await roomTypesResponse.json();
            console.log('✅ Room Types:', roomTypes);
        } else {
            console.log('❌ Room types endpoint failed:', roomTypesResponse.status);
        }
        
        // Test all static data endpoint
        const allDataResponse = await fetch(`${baseUrl}/staticdata/all`, {
            headers: {
                'Authorization': 'Bearer YOUR_JWT_TOKEN_HERE'
            }
        });
        
        if (allDataResponse.ok) {
            const allData = await allDataResponse.json();
            console.log('✅ All Static Data loaded successfully');
            console.log('   - Room Types:', allData.roomTypes?.length || 0);
            console.log('   - Room Statuses:', allData.roomStatuses?.length || 0);
            console.log('   - Reservation Statuses:', allData.reservationStatuses?.length || 0);
            console.log('   - Reservation Sources:', allData.reservationSources?.length || 0);
        } else {
            console.log('❌ All static data endpoint failed:', allDataResponse.status);
        }
        
    } catch (error) {
        console.error('❌ Static data caching test failed:', error);
    }
}

// Test caching performance by making multiple requests
async function testCachingPerformance() {
    console.log('Testing Caching Performance...');
    
    try {
        const endpoint = `${baseUrl}/hotels`;
        const iterations = 5;
        const times = [];
        
        for (let i = 0; i < iterations; i++) {
            const startTime = performance.now();
            
            const response = await fetch(endpoint, {
                headers: {
                    'Authorization': 'Bearer YOUR_JWT_TOKEN_HERE'
                }
            });
            
            const endTime = performance.now();
            const duration = endTime - startTime;
            times.push(duration);
            
            if (response.ok) {
                console.log(`✅ Request ${i + 1}: ${duration.toFixed(2)}ms`);
            } else {
                console.log(`❌ Request ${i + 1} failed: ${response.status}`);
            }
            
            // Small delay between requests
            await new Promise(resolve => setTimeout(resolve, 100));
        }
        
        const averageTime = times.reduce((a, b) => a + b, 0) / times.length;
        const firstRequestTime = times[0];
        const subsequentAverage = times.slice(1).reduce((a, b) => a + b, 0) / (times.length - 1);
        
        console.log('📊 Performance Summary:');
        console.log(`   - First request: ${firstRequestTime.toFixed(2)}ms`);
        console.log(`   - Subsequent average: ${subsequentAverage.toFixed(2)}ms`);
        console.log(`   - Overall average: ${averageTime.toFixed(2)}ms`);
        console.log(`   - Cache improvement: ${((firstRequestTime - subsequentAverage) / firstRequestTime * 100).toFixed(1)}%`);
        
    } catch (error) {
        console.error('❌ Caching performance test failed:', error);
    }
}

// Test response time headers
async function testResponseTimeHeaders() {
    console.log('Testing Response Time Headers...');
    
    try {
        const response = await fetch(`${baseUrl}/hotels`, {
            headers: {
                'Authorization': 'Bearer YOUR_JWT_TOKEN_HERE'
            }
        });
        
        const responseTime = response.headers.get('X-Response-Time');
        
        if (responseTime) {
            console.log('✅ Response Time Header:', responseTime);
        } else {
            console.log('❌ Response Time Header not found');
        }
        
    } catch (error) {
        console.error('❌ Response time header test failed:', error);
    }
}

// Run all tests
async function runAllTests() {
    console.log('🚀 Starting Caching and Performance Tests...\n');
    
    await testResponseTimeHeaders();
    console.log('');
    
    await testStaticDataCaching();
    console.log('');
    
    await testCachingPerformance();
    console.log('');
    
    await testPerformanceMonitoring();
    console.log('');
    
    console.log('✅ All tests completed!');
    console.log('\n📝 Notes:');
    console.log('- Replace YOUR_JWT_TOKEN_HERE with a valid JWT token');
    console.log('- Ensure the API is running on https://localhost:7001');
    console.log('- Redis should be running for distributed caching');
    console.log('- Check the application logs for performance metrics');
}

// Export for Node.js or run in browser
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        testPerformanceMonitoring,
        testStaticDataCaching,
        testCachingPerformance,
        testResponseTimeHeaders,
        runAllTests
    };
} else {
    // Run tests if in browser
    runAllTests();
}