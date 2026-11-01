export interface CurrentUser {
  id: string;
  displayName: string;
  email: string;
  isActive: boolean;
  roles: string[];
  permissions: string[];
  mustChangePassword: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
