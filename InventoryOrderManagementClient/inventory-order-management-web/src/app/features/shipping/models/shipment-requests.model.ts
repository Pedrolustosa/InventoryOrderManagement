import { ShipmentStatus } from './shipment.enums';

export const SHIPMENT_PAGE_SIZE = 20;
export const CARRIER_NAME_MAX_LENGTH = 200;
export const TRACKING_CODE_MAX_LENGTH = 80;
export const SHIPMENT_NOTES_MAX_LENGTH = 500;
export const FAILURE_REASON_MAX_LENGTH = 300;

export interface CreateShipmentRequest {
  orderId: string;
  carrierName: string | null;
  trackingCode: string | null;
  notes: string | null;
}

export interface UpdateShipmentRequest {
  carrierName: string | null;
  trackingCode: string | null;
  notes: string | null;
}

export interface ShipmentNotesRequest {
  notes: string | null;
}

export interface FailShipmentRequest {
  failureReason: string;
  notes: string | null;
}

export interface ShipmentFilters {
  status: ShipmentStatus | null;
  orderId: string | null;
  customerId: string | null;
  carrierName: string | null;
  trackingCode: string | null;
  startDateUtc: string | null;
  endDateUtc: string | null;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export function hasShipmentFilters(filters: ShipmentFilters): boolean {
  return Boolean(
    filters.status ||
      filters.orderId ||
      filters.customerId ||
      filters.carrierName ||
      filters.trackingCode ||
      filters.startDateUtc ||
      filters.endDateUtc,
  );
}

export function emptyShipmentFilters(): ShipmentFilters {
  return {
    status: null,
    orderId: null,
    customerId: null,
    carrierName: null,
    trackingCode: null,
    startDateUtc: null,
    endDateUtc: null,
    page: 1,
    pageSize: SHIPMENT_PAGE_SIZE,
  };
}
