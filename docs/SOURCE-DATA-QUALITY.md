# Source data quality notes

The public repository contains only sanitized technical reference data. Customer names, addresses, phone numbers, email addresses, bank details, historical prices and full quotation documents are not committed.

## Commercial controls

- Historical quotations are treated as private inputs and must be stored outside this public repository.
- A current price must be explicitly approved, versioned and loaded from private configuration before use.
- Transport, AMF panel, installation and other charges remain separate components.
- GST is recalculated from components; source totals tolerate at most one rupee rounding variance.

## Detected source conflicts

- The Cummins CI500D5P brochure table states 500/440 kVA/kWe, which does not match a 0.8 power factor conversion. It is flagged for technical review.
