import { CustomerAddress } from './customer-address.model';
import { CustomerType } from './customer-type.model';

export interface CreateCustomerRequest {
  customerType: CustomerType;
  name: string;
  taxIdentifier: string | null;
  email: string | null;
  phone: string | null;
  address: CustomerAddress | null;
}
