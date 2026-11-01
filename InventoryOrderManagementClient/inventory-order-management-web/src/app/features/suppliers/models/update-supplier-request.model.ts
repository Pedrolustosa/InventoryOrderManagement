export interface UpdateSupplierRequest {
  name: string;
  taxIdentifier: string | null;
  contactName: string | null;
  email: string | null;
  phone: string | null;
  notes: string | null;
}
