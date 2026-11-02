export const SHIPMENT_STATUSES = [
  'Preparing',
  'Shipped',
  'InTransit',
  'Delivered',
  'Failed',
] as const;

export type ShipmentStatus = (typeof SHIPMENT_STATUSES)[number];

export function shipmentStatusLabel(status: ShipmentStatus): string {
  switch (status) {
    case 'Preparing':
      return 'Preparing';
    case 'Shipped':
      return 'Shipped';
    case 'InTransit':
      return 'In transit';
    case 'Delivered':
      return 'Delivered';
    case 'Failed':
      return 'Failed';
  }
}

export function shipmentStatusClass(status: ShipmentStatus): string {
  switch (status) {
    case 'Preparing':
      return 'badge-inactive';
    case 'Shipped':
      return 'inline-flex rounded-full bg-sky-100 px-2.5 py-1 text-xs font-semibold text-sky-900';
    case 'InTransit':
      return 'inline-flex rounded-full bg-amber-100 px-2.5 py-1 text-xs font-semibold text-amber-900';
    case 'Delivered':
      return 'inline-flex rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-semibold text-emerald-900';
    case 'Failed':
      return 'inline-flex rounded-full bg-danger/10 px-2.5 py-1 text-xs font-semibold text-danger';
  }
}
