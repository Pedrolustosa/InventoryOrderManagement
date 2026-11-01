import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/guards/auth.guards';
import { AssignRolesPage } from './users/pages/assign-roles-page/assign-roles-page';
import { UserDetailsPage } from './users/pages/user-details-page/user-details-page';
import { UserFormPage } from './users/pages/user-form-page/user-form-page';
import { UsersPage } from './users/pages/users-page/users-page';
import { RoleDetailsPage } from './roles/pages/role-details-page/role-details-page';
import { RoleFormPage } from './roles/pages/role-form-page/role-form-page';
import { RolesPage } from './roles/pages/roles-page/roles-page';

export const USER_ROUTES: Routes = [
  { path: '', component: UsersPage },
  { path: 'new', component: UserFormPage, canActivate: [permissionGuard('Users.Create')] },
  { path: ':id', component: UserDetailsPage },
  { path: ':id/edit', component: UserFormPage, canActivate: [permissionGuard('Users.Update')] },
  { path: ':id/roles', component: AssignRolesPage, canActivate: [permissionGuard('Users.Update')] },
];

export const ROLE_ROUTES: Routes = [
  { path: '', component: RolesPage },
  { path: 'new', component: RoleFormPage, canActivate: [permissionGuard('Roles.Create')] },
  { path: ':id', component: RoleDetailsPage },
  { path: ':id/edit', component: RoleFormPage, canActivate: [permissionGuard('Roles.Update')] },
];
