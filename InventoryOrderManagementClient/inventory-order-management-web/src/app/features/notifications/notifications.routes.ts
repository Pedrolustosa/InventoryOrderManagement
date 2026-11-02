import { Routes } from '@angular/router';
import { NotificationListPage } from './pages/notification-list/notification-list-page';

export const NOTIFICATIONS_ROUTES: Routes = [
  {
    path: '',
    component: NotificationListPage,
    data: { title: 'ROUTES.NOTIFICATIONS', breadcrumb: 'ROUTES.NOTIFICATIONS' },
  },
];
