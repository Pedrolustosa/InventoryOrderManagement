export interface SecurityAuditLog {
  id: string;
  actorUserId: string | null;
  targetUserId: string | null;
  action: string;
  entityType: string;
  entityId: string | null;
  outcome: string;
  occurredAtUtc: string;
  correlationId: string | null;
  details: string | null;
}

export interface AuditQuery {
  actorUserId?: string;
  targetUserId?: string;
  action?: string;
  entityType?: string;
  startDateUtc?: string;
  endDateUtc?: string;
  page: number;
  pageSize: number;
}
