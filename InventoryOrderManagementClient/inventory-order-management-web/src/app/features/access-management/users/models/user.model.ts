export interface RoleAssignment {
  id: string;
  name: string;
}

export interface UserAccount {
  id: string;
  displayName: string;
  email: string;
  phoneNumber: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
  roles: RoleAssignment[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
  lastLoginAtUtc: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface UserListQuery {
  name?: string;
  email?: string;
  isActive?: boolean;
  roleId?: string;
  page: number;
  pageSize: number;
}

export interface SaveUserRequest {
  displayName: string;
  email: string;
  phoneNumber: string | null;
}

export interface CreateUserRequest extends SaveUserRequest {
  password: string;
  roleIds: string[];
}
