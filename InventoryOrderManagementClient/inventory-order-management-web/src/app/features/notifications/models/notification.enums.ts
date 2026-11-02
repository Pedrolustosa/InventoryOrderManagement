export enum NotificationType {
  LowStockDetected = 'LowStockDetected',
  OrderConfirmed = 'OrderConfirmed',
  ShipmentFailed = 'ShipmentFailed',
  ReturnAwaitingInspection = 'ReturnAwaitingInspection',
}

export enum NotificationSeverity {
  Info = 'Info',
  Warning = 'Warning',
  Critical = 'Critical',
}

export const NOTIFICATION_TYPES = Object.values(NotificationType);

export const NOTIFICATION_SEVERITIES = Object.values(NotificationSeverity);

export function notificationTypeLabelKey(type: NotificationType): string {
  return `NOTIFICATIONS.TYPES.${type}`;
}

export function notificationSeverityLabelKey(severity: NotificationSeverity): string {
  return `NOTIFICATIONS.SEVERITY.${severity}`;
}
