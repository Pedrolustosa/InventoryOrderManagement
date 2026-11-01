export interface Role {
  id: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  permissions: string[];
}

export interface SaveRoleRequest {
  name: string;
  description: string | null;
}
