import { CustomerAddress } from './customer-address.model';
import { CustomerType } from './customer-type.model';

export interface CustomerResponse {
  id: string;
  customerType: CustomerType;
  name: string;
  taxIdentifier: string | null;
  email: string | null;
  phone: string | null;
  address: CustomerAddress | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}
