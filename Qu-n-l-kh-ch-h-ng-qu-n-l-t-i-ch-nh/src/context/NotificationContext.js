import React, { createContext, useContext, useEffect, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useAuth } from './AuthContext';
import axios from 'axios';
import { notification } from 'antd';
import { API_BASE_URL } from '../config/api';

const NotificationContext = createContext();

export const NotificationProvider = ({ children }) => {
    const { user } = useAuth();
    const [connection, setConnection] = useState(null);
    const [notifications, setNotifications] = useState([]);
    const [unreadCount, setUnreadCount] = useState(0);

    const token = localStorage.getItem('authToken');

    // Initialize SignalR connection
    useEffect(() => {
        if (!token) {
            setConnection(null);
            return;
        }

        // Use the base URL for the hub as well, removing /api if needed or appending /hubs
        const hubUrl = `${API_BASE_URL.replace(/\/$/, '')}/hubs/notifications`;

        const newConnection = new HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        setConnection(newConnection);
    }, [token]);

    // Start connection and listeners
    useEffect(() => {
        if (connection) {
            connection.start()
                .then(() => {
                    console.log('Connected to Notification Hub');
                    
                    connection.on('ReceiveNotification', (notif) => {
                        setNotifications(prev => [notif, ...prev]);
                        setUnreadCount(prev => prev + 1);
                        
                        notification.info({
                            message: notif.title,
                            description: notif.message,
                            placement: 'topRight',
                            duration: 4.5,
                        });
                    });
                })
                .catch(e => console.log('Connection failed: ', e));
                
            return () => {
                connection.stop();
            };
        }
    }, [connection]);

    // Fetch initial notifications
    const fetchNotifications = async () => {
        if (!user) return;
        try {
            const res = await axios.get('/api/notifications');
            setNotifications(res.data.notifications);
            setUnreadCount(res.data.unreadCount);
        } catch (error) {
            console.error("Failed to fetch notifications", error);
        }
    };

    useEffect(() => {
        if (user) {
            fetchNotifications();
        }
    }, [user]);

    const refreshNotifications = () => {
        fetchNotifications();
    };

    const markAsRead = async (id) => {
        try {
            await axios.post(`/api/notifications/mark-read/${id}`);
            setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
            setUnreadCount(prev => Math.max(0, prev - 1));
        } catch (error) {
            console.error("Failed to mark as read", error);
        }
    };

    const markAllAsRead = async () => {
        try {
            await axios.post(`/api/notifications/mark-all-read`);
            setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
            setUnreadCount(0);
        } catch (error) {
            console.error("Failed to mark all as read", error);
        }
    };

    return (
        <NotificationContext.Provider value={{ 
            notifications, 
            unreadCount, 
            markAsRead, 
            markAllAsRead,
            refreshNotifications 
        }}>
            {children}
        </NotificationContext.Provider>
    );
};

export const useNotification = () => useContext(NotificationContext);
