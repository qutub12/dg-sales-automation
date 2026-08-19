# Private approved price catalogue

Approved prices are runtime business configuration and must not be committed to this public repository.
Store the JSON file in a private secret volume or protected deployment directory and set
`Pricing__ApprovedCataloguePath` to its absolute path.

Each item has this shape:

```json
[
  {
    "version": "owner-approved-version",
    "brand": "Approved brand",
    "gensetModel": "Approved model",
    "kva": 0,
    "phaseCount": 3,
    "basePrice": 0,
    "standardMarkup": 0,
    "transportCharge": 0,
    "installationCharge": 0,
    "accessoryCharge": 0,
    "gstPercent": 18,
    "effectiveFrom": "2026-01-01",
    "effectiveTo": null,
    "isActive": false
  }
]
```

## Approval rules

- The business owner assigns a new `version` whenever any commercial value changes.
- Only active entries inside their effective date range are considered.
- kVA, phase and optional preferred brand must match the captured requirement.
- The generated quotation stores the version and calculated totals as an immutable snapshot.
- Discounts, non-standard terms, delivery promises, incomplete sizing, or validation flags always require review.
