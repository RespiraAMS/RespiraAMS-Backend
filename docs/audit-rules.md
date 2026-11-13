# Audit logs and Clinical rollback

Clinical, Patient, and Analysis record data changes in an `audit_entries` table
in the same database transaction as the changed data. Each entry includes the
actor and role, action, entity type and id, timestamp, correlation and trace
ids, request path, changed fields, and before/after snapshots where available.
Entries from one `SaveChanges` call share a `changeSetId`; rolling back an entry
therefore previews and reverses the whole set. Audit rows are immutable history:
a rollback writes a separate `Clinical.AuditRollback` entry.

The services forward audit events asynchronously to Saga's audit log. Use the
Saga endpoint to search across forwarded events:

```http
GET /api/v1/audit-logs?entityType=Respira.Clinical.Domain.Entities.Antibiotic&page=1&pageSize=50
```

Saga results expose an `id` that is the source service's audit entry id. Use it
with the Clinical endpoints below to inspect the local record and preview or
apply a rollback:

```http
GET  /api/v1/clinical/audit-logs/{id}
POST /api/v1/clinical/audit-logs/{id}/rollback/preview
POST /api/v1/clinical/audit-logs/{id}/rollback
```

The endpoints require the `Admin` role. The gateway authenticates the caller
and forwards identity headers; ServiceDefaults turns those headers into claims,
then Clinical authorization applies its service-specific role rules. Review the
preview before applying. Preview returns each proposed change and any conflicts;
the apply endpoint returns `409 Conflict` if current data no longer matches the
audited after-state. This prevents overwriting later edits. The transaction
restores updates and deletes, removes rows created by the change set, and
reverses many-to-many relationship changes. Rollback audit entries cannot
themselves be rolled back.

Audit forwarding is asynchronous, so a newly committed change may take a short
time to appear in Saga search. Rollback reads the local Clinical database and
can use the local audit entry as soon as it is committed. Snapshots omit
non-owned relationship graphs; the Clinical rollback handles captured
many-to-many join rows separately.
