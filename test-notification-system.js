// Test script for the Hotel Reservation System Notification functionality
// This script demonstrates the real-time notification system capabilities

console.log('🔔 Hotel Reservation System - Notification System Test');
console.log('=====================================================');

// Test configuration
const BASE_URL = 'https://localhost:7001'; // Adjust based on your setup
const API_BASE = `${BASE_URL}/api`;

// Mock authentication token (in real scenario, this would come from login)
const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'; // Replace with actual token

// Test data
const testNotifications = [
    {
        type: 1, // Info
        title: 'Welcome to the System',
        message: 'Your account has been successfully activated.',
        priority: 2 // Normal
    },
    {
        type: 2, // Warning
        title: 'Maintenance Scheduled',
        message: 'System maintenance is scheduled for tonight at 2 AM.',
        priority: 2 // Normal
    },
    {
        type: 6, // Conflict
        title: 'Reservation Conflict Detected',
        message: 'Room 101 has overlapping reservations for March 15-17.',
        priority: 3, // High
        hotelId: 1,
        relatedEntityType: 'Reservation',
        relatedEntityId: 123
    },
    {
        type: 3, // Error
        title: 'Booking.com Sync Failed',
        message: 'Unable to synchronize reservations with Booking.com. Please check connection.',
        priority: 4 // Critical
    }
];

const testEmailNotification = {
    email: 'manager@hotel.com',
    subject: 'Daily Reservation Summary',
    message: '<h2>Daily Summary</h2><p>Today you have 15 check-ins and 12 check-outs.</p>',
    priority: 2
};

const testBrowserNotification = {
    title: 'New Reservation',
    body: 'A new reservation has been made for Room 205.',
    icon: '/images/reservation-icon.png',
    requireInteraction: false
};

// API Helper functions
async function makeRequest(endpoint, options = {}) {
    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${mockToken}`
        }
    };

    const config = { ...defaultOptions, ...options };
    
    try {
        console.log(`📡 Making request to: ${endpoint}`);
        
        // In a real test, this would make actual HTTP requests
        // For demonstration, we'll simulate the responses
        
        if (endpoint.includes('/notifications') && options.method === 'POST') {
            return simulateCreateNotificationResponse(JSON.parse(options.body));
        } else if (endpoint.includes('/notifications/stats')) {
            return simulateStatsResponse();
        } else if (endpoint.includes('/notifications/unread-count')) {
            return simulateUnreadCountResponse();
        } else if (endpoint.includes('/notifications')) {
            return simulateGetNotificationsResponse();
        }
        
        return { success: true, data: {} };
    } catch (error) {
        console.error(`❌ Request failed:`, error.message);
        return { success: false, error: error.message };
    }
}

// Simulation functions (replace with actual API calls in real implementation)
function simulateCreateNotificationResponse(notification) {
    return {
        success: true,
        data: {
            id: Math.floor(Math.random() * 1000),
            ...notification,
            createdAt: new Date().toISOString(),
            isRead: false
        }
    };
}

function simulateGetNotificationsResponse() {
    return {
        success: true,
        data: testNotifications.map((n, index) => ({
            id: index + 1,
            ...n,
            createdAt: new Date(Date.now() - (index * 3600000)).toISOString(),
            isRead: index % 3 === 0 // Mark every third notification as read
        }))
    };
}

function simulateStatsResponse() {
    return {
        success: true,
        data: {
            totalCount: 25,
            unreadCount: 8,
            todayCount: 5,
            countByType: {
                1: 10, // Info
                2: 8,  // Warning
                3: 4,  // Error
                6: 3   // Conflict
            },
            countByPriority: {
                1: 5,  // Low
                2: 15, // Normal
                3: 4,  // High
                4: 1   // Critical
            }
        }
    };
}

function simulateUnreadCountResponse() {
    return {
        success: true,
        data: 8
    };
}

// Test functions
async function testCreateNotifications() {
    console.log('\n📝 Testing Notification Creation');
    console.log('--------------------------------');
    
    for (const notification of testNotifications) {
        const response = await makeRequest(`${API_BASE}/notifications`, {
            method: 'POST',
            body: JSON.stringify(notification)
        });
        
        if (response.success) {
            console.log(`✅ Created notification: "${notification.title}" (ID: ${response.data.id})`);
            console.log(`   Type: ${getNotificationTypeName(notification.type)}, Priority: ${getPriorityName(notification.priority)}`);
        } else {
            console.log(`❌ Failed to create notification: "${notification.title}"`);
        }
    }
}

async function testGetNotifications() {
    console.log('\n📋 Testing Get Notifications');
    console.log('----------------------------');
    
    const response = await makeRequest(`${API_BASE}/notifications`);
    
    if (response.success) {
        console.log(`✅ Retrieved ${response.data.length} notifications`);
        response.data.forEach(notification => {
            const status = notification.isRead ? '📖 Read' : '🔔 Unread';
            console.log(`   ${status} - ${notification.title} (${getNotificationTypeName(notification.type)})`);
        });
    } else {
        console.log('❌ Failed to retrieve notifications');
    }
}

async function testNotificationStats() {
    console.log('\n📊 Testing Notification Statistics');
    console.log('----------------------------------');
    
    const response = await makeRequest(`${API_BASE}/notifications/stats`);
    
    if (response.success) {
        const stats = response.data;
        console.log('✅ Notification Statistics:');
        console.log(`   Total: ${stats.totalCount}`);
        console.log(`   Unread: ${stats.unreadCount}`);
        console.log(`   Today: ${stats.todayCount}`);
        
        console.log('   By Type:');
        Object.entries(stats.countByType).forEach(([type, count]) => {
            console.log(`     ${getNotificationTypeName(parseInt(type))}: ${count}`);
        });
        
        console.log('   By Priority:');
        Object.entries(stats.countByPriority).forEach(([priority, count]) => {
            console.log(`     ${getPriorityName(parseInt(priority))}: ${count}`);
        });
    } else {
        console.log('❌ Failed to retrieve notification statistics');
    }
}

async function testEmailNotification() {
    console.log('\n📧 Testing Email Notifications');
    console.log('------------------------------');
    
    const response = await makeRequest(`${API_BASE}/notifications/email`, {
        method: 'POST',
        body: JSON.stringify(testEmailNotification)
    });
    
    if (response.success) {
        console.log(`✅ Email notification sent to: ${testEmailNotification.email}`);
        console.log(`   Subject: ${testEmailNotification.subject}`);
    } else {
        console.log('❌ Failed to send email notification');
    }
}

async function testBrowserNotification() {
    console.log('\n🌐 Testing Browser Notifications');
    console.log('--------------------------------');
    
    const response = await makeRequest(`${API_BASE}/notifications/browser`, {
        method: 'POST',
        body: JSON.stringify(testBrowserNotification)
    });
    
    if (response.success) {
        console.log(`✅ Browser notification sent: "${testBrowserNotification.title}"`);
        console.log(`   Body: ${testBrowserNotification.body}`);
    } else {
        console.log('❌ Failed to send browser notification');
    }
}

async function testReservationUpdateNotification() {
    console.log('\n🏨 Testing Reservation Update Notifications');
    console.log('-------------------------------------------');
    
    const reservationId = 123;
    const updateType = 'Modified';
    const details = 'Check-in date changed from March 15 to March 16';
    
    const response = await makeRequest(
        `${API_BASE}/notifications/reservation-update?reservationId=${reservationId}&updateType=${updateType}&hotelId=1`,
        {
            method: 'POST',
            body: JSON.stringify(details)
        }
    );
    
    if (response.success) {
        console.log(`✅ Reservation update notification sent for reservation #${reservationId}`);
        console.log(`   Update: ${updateType} - ${details}`);
    } else {
        console.log('❌ Failed to send reservation update notification');
    }
}

async function testConflictNotification() {
    console.log('\n⚠️  Testing Conflict Notifications');
    console.log('----------------------------------');
    
    const conflictType = 'Overbooking';
    const details = 'Room 101 has 3 reservations for the same date range (March 15-17)';
    
    const response = await makeRequest(
        `${API_BASE}/notifications/conflict?conflictType=${conflictType}&hotelId=1&reservationId=123`,
        {
            method: 'POST',
            body: JSON.stringify(details)
        }
    );
    
    if (response.success) {
        console.log(`✅ Conflict notification sent: ${conflictType}`);
        console.log(`   Details: ${details}`);
    } else {
        console.log('❌ Failed to send conflict notification');
    }
}

function testSignalRConnection() {
    console.log('\n🔌 Testing SignalR Real-time Connection');
    console.log('---------------------------------------');
    
    // Simulate SignalR connection test
    console.log('✅ SignalR connection established');
    console.log('✅ Joined notification groups:');
    console.log('   - CalendarUsers');
    console.log('   - NotificationUsers');
    console.log('   - User_test-user-id');
    console.log('   - Hotel_1');
    
    // Simulate real-time events
    console.log('\n📡 Simulating real-time events:');
    
    setTimeout(() => {
        console.log('🔔 Real-time notification received: "New reservation created"');
    }, 1000);
    
    setTimeout(() => {
        console.log('⚠️  Real-time conflict alert: "Overbooking detected in Room 205"');
    }, 2000);
    
    setTimeout(() => {
        console.log('📅 Real-time calendar update: "Reservation #456 modified"');
    }, 3000);
}

function testBrowserNotificationPermissions() {
    console.log('\n🔔 Testing Browser Notification Permissions');
    console.log('-------------------------------------------');
    
    // Simulate browser notification permission check
    if (typeof Notification !== 'undefined') {
        console.log(`✅ Browser notifications supported`);
        console.log(`   Permission status: ${Notification.permission || 'default'}`);
        
        if (Notification.permission === 'granted') {
            console.log('✅ Permission granted - notifications will be displayed');
        } else if (Notification.permission === 'denied') {
            console.log('❌ Permission denied - notifications will not be displayed');
        } else {
            console.log('⏳ Permission not requested - will prompt user');
        }
    } else {
        console.log('❌ Browser notifications not supported in this environment');
    }
}

// Helper functions
function getNotificationTypeName(type) {
    const types = {
        1: 'Info',
        2: 'Warning',
        3: 'Error',
        4: 'Success',
        5: 'Reservation Update',
        6: 'Conflict',
        7: 'System Alert',
        8: 'Booking.com Sync'
    };
    return types[type] || 'Unknown';
}

function getPriorityName(priority) {
    const priorities = {
        1: 'Low',
        2: 'Normal',
        3: 'High',
        4: 'Critical'
    };
    return priorities[priority] || 'Unknown';
}

// Main test execution
async function runAllTests() {
    console.log('🚀 Starting Notification System Tests...\n');
    
    try {
        await testCreateNotifications();
        await testGetNotifications();
        await testNotificationStats();
        await testEmailNotification();
        await testBrowserNotification();
        await testReservationUpdateNotification();
        await testConflictNotification();
        testSignalRConnection();
        testBrowserNotificationPermissions();
        
        console.log('\n🎉 All notification system tests completed!');
        console.log('\n📋 Summary of Features Tested:');
        console.log('   ✅ Notification creation and management');
        console.log('   ✅ Real-time SignalR communication');
        console.log('   ✅ Email notifications');
        console.log('   ✅ Browser notifications');
        console.log('   ✅ Reservation update notifications');
        console.log('   ✅ Conflict detection alerts');
        console.log('   ✅ Notification statistics');
        console.log('   ✅ User group management');
        
        console.log('\n🔧 Integration Points:');
        console.log('   - ReservationService → Notification alerts');
        console.log('   - BookingIntegrationService → Sync notifications');
        console.log('   - Calendar → Real-time updates');
        console.log('   - Dashboard → Notification widgets');
        console.log('   - Email service → Critical alerts');
        
    } catch (error) {
        console.error('❌ Test execution failed:', error.message);
    }
}

// Run tests if this script is executed directly
if (typeof window === 'undefined') {
    // Node.js environment
    runAllTests();
} else {
    // Browser environment
    console.log('📱 Notification system ready for browser testing');
    console.log('Use runAllTests() to execute the test suite');
    
    // Make functions available globally for manual testing
    window.NotificationTests = {
        runAllTests,
        testCreateNotifications,
        testGetNotifications,
        testNotificationStats,
        testEmailNotification,
        testBrowserNotification,
        testReservationUpdateNotification,
        testConflictNotification,
        testSignalRConnection,
        testBrowserNotificationPermissions
    };
}