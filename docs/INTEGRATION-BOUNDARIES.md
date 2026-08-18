# Integration boundaries

Business rules do not depend directly on Exotel, Vapi, Sarvam or Meta APIs. Provider adapters implement the interfaces under `Integrations`.

## Voice

The voice adapter receives an already-qualified call job and may start the external call. It must return only provider identity and status. Conversation extraction writes structured requirements through application commands; the language model cannot calculate capacity or price.

## WhatsApp

The WhatsApp adapter sends only an already-generated, approved quotation document using an approved template. Access tokens remain in deployment secrets, never in source control.

## Activation prerequisites

- programmable Indian telephony number and verified business identity
- voice provider API credentials and webhook signing secret
- Meta business verification, WhatsApp coexistence onboarding and approved templates
- public HTTPS webhook endpoints

Until these exist, adapters remain disabled and no external call or message is attempted.
