// Notifications.js - Handles real-time deployment notifications via SignalR

class NotificationManager {
    constructor() {
        this.notifications = new Map();
        this.connection = null;
        this.initializeSignalR();
        this.bindEvents();
    }

    async initializeSignalR() {
        // Only initialize if user is authenticated
        if (!document.querySelector('#notificationDropdown')) {
            return;
        }

        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/deploymentHub")
                .withAutomaticReconnect()
                .build();

            // Handle deployment status updates
            this.connection.on("DeploymentStatusUpdate", (notification) => {
                this.handleNotification(notification);
            });

            // Start the connection
            await this.connection.start();
            
            // Join user group for personalized notifications
            await this.connection.invoke("JoinUserGroup");
            
            console.log("SignalR Connected");
        } catch (err) {
            console.error("SignalR Connection Error: ", err);
            // Retry connection after 5 seconds
            setTimeout(() => this.initializeSignalR(), 5000);
        }
    }

    handleNotification(notification) {
        console.log("Received notification:", notification);
        
        // Store or update notification
        this.notifications.set(notification.id || notification.deploymentName, notification);
        
        // Update UI
        this.updateNotificationUI();
        
        // Show browser notification if supported and deployment is complete
        if (notification.isCompleted && 'Notification' in window) {
            this.showBrowserNotification(notification);
        }
    }

    updateNotificationUI() {
        const badge = document.getElementById('notificationBadge');
        const notificationList = document.getElementById('notificationList');
        
        if (!badge || !notificationList) return;

        const notificationArray = Array.from(this.notifications.values());
        const unreadCount = notificationArray.length;

        // Update badge
        if (unreadCount > 0) {
            badge.textContent = unreadCount;
            badge.style.display = 'inline';
        } else {
            badge.style.display = 'none';
        }

        // Update notification list
        if (notificationArray.length === 0) {
            notificationList.innerHTML = `
                <div class="px-3 py-4 text-center text-muted">
                    <i class="fas fa-inbox fs-4 mb-2"></i>
                    <p class="mb-0">No notifications</p>
                </div>
            `;
        } else {
            // Sort by start time, newest first
            notificationArray.sort((a, b) => new Date(b.startTime) - new Date(a.startTime));
            
            notificationList.innerHTML = notificationArray.map(notification => 
                this.createNotificationHTML(notification)
            ).join('');
        }
    }

    createNotificationHTML(notification) {
        const statusClass = notification.isSuccessful ? 'notification-success' : 
                           notification.hasError ? 'notification-error' : 'notification-running';
        
        const statusIcon = notification.isSuccessful ? 'fas fa-check-circle text-success' :
                          notification.hasError ? 'fas fa-times-circle text-danger' :
                          'fas fa-spinner fa-spin text-info';

        const duration = notification.duration ? this.formatDuration(notification.duration) : '';
        const startTime = new Date(notification.startTime).toLocaleString();
        
        return `
            <div class="notification-item ${statusClass} position-relative" data-id="${notification.id || notification.deploymentName}">
                <button type="button" class="notification-close" onclick="notificationManager.removeNotification('${notification.id || notification.deploymentName}')">
                    <i class="fas fa-times"></i>
                </button>
                <div class="notification-header">
                    <i class="${statusIcon} me-2"></i>
                    ${notification.deploymentName}
                </div>
                <div class="notification-details">
                    Status: <strong>${notification.status}</strong>
                </div>
                <div class="notification-details">
                    Resource Group: ${notification.resourceGroup}
                </div>
                ${duration ? `<div class="notification-details">Duration: ${duration}</div>` : ''}
                <div class="notification-time">
                    Started: ${startTime}
                </div>
                ${notification.message ? `<div class="notification-details text-muted">${notification.message}</div>` : ''}
            </div>
        `;
    }

    formatDuration(duration) {
        // Duration comes as a TimeSpan string like "00:05:30.1234567"
        if (typeof duration === 'string') {
            const parts = duration.split(':');
            if (parts.length >= 3) {
                const hours = parseInt(parts[0]);
                const minutes = parseInt(parts[1]);
                const seconds = Math.floor(parseFloat(parts[2]));
                
                if (hours > 0) {
                    return `${hours}h ${minutes}m ${seconds}s`;
                } else if (minutes > 0) {
                    return `${minutes}m ${seconds}s`;
                } else {
                    return `${seconds}s`;
                }
            }
        }
        return duration;
    }

    removeNotification(id) {
        this.notifications.delete(id);
        this.updateNotificationUI();
    }

    clearAllNotifications() {
        this.notifications.clear();
        this.updateNotificationUI();
    }

    showBrowserNotification(notification) {
        if (Notification.permission === 'granted') {
            const title = notification.isSuccessful ? 
                '✅ Deployment Successful' : 
                '❌ Deployment Failed';
            
            const body = `${notification.deploymentName} - ${notification.status}`;
            
            new Notification(title, {
                body: body,
                icon: '/favicon.ico'
            });
        } else if (Notification.permission !== 'denied') {
            Notification.requestPermission().then(permission => {
                if (permission === 'granted') {
                    this.showBrowserNotification(notification);
                }
            });
        }
    }

    bindEvents() {
        document.addEventListener('DOMContentLoaded', () => {
            const clearAllBtn = document.getElementById('clearAllNotifications');
            if (clearAllBtn) {
                clearAllBtn.addEventListener('click', () => {
                    this.clearAllNotifications();
                });
            }
        });
    }
}

// Initialize the notification manager
const notificationManager = new NotificationManager();

// Make it globally available for button clicks
window.notificationManager = notificationManager;