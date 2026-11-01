import { CustomerType } from './customer-type.model';

export interface CustomerFilters {
  name?: string | null;
  customerType?: CustomerType | null;
  isActive?: boolean | null;
}
