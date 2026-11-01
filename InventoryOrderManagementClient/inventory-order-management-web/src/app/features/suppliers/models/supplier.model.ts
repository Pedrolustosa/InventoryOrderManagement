export interface SupplierResponse {
  id: string;
  name: string;
  taxIdentifier: string | null;
  contactName: string | null;
  email: string | null;
  phone: string | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}
