export interface CancelOrderRequest {
  reason: string;
}

export const CANCELLATION_REASON_MAX_LENGTH = 300;
