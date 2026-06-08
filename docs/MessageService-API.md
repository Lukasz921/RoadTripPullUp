# MessageService API

Base URL: `/api/v1/message`
Autoryzacja: JWT (claim `NameIdentifier` jako GUID użytkownika).

---

### POST /api/v1/message/conversations
Opis: tworzy nową konwersację.

Request (body):
```json
{
  "Type": "Direct | Group",
  "TripId": "<guid>"
  "Title": "string?",
  "Date": "2026-05-24T12:00:00Z?",
  "Participants": ["<guid>", ...]
}
```

Response (201 Created) — body:
```json
{
  "conversationId": "<guid>"
}
```

Błędy: 400/401/422

---

### GET /api/v1/message/conversations
Opis: pobiera listę konwersacji dla zalogowanego użytkownika.

Query params (opcjonalne):
- `fromConversation` (int)
- `toConversation` (int)

Response (200 OK) — body: lista obiektów `ConversationDto`:
```json
[
  {
    "ConversationId": "<guid>",
    "Type": "Direct | Group",
    "TripId": "<guid>",
    "Name": "string?",
    "Date": "2026-05-24T12:00:00Z?",
    "Participants": ["<guid>", ...],
    "LastMessageId": "<guid>",
    "LastMessagePreview": "string",
    "LastMessageCreatedAt": "2026-05-24T12:00:00Z"
  },
  ...
]
```
Błędy: 400

---

### GET /api/v1/message/conversations/{conversationId}
Opis: pobiera szczegóły jednej konwersacji.

Response (200 OK) — body: `ConversationDto`:
```json
{
  "ConversationId": "<guid>",
  "Type": "Direct | Group",
  "Name": "string?",
  "Date": "2026-05-24T12:00:00Z?",
  "Participants": ["<guid>", ...],
  "LastMessageId": "<guid>",
  "LastMessagePreview": "string",
  "LastMessageCreatedAt": "2026-05-24T12:00:00Z"
}
```
Błędy: 404

---
### GET /api/v1/message/conversations/byTripId/group/{tripId:guid}
Opis: pobiera konwersację grupową dla danego tripu.

Response (200 OK) — body: `ConversationDto` (jak wyżej).

Błędy: 403/404

---
### GET /api/v1/message/conversations/byTripId/direct/{tripId:guid}
Opis: pobiera listę konwersacji bezpośrednich dla danego tripu, w których uczestniczy zalogowany użytkownik.

Response (200 OK) — body: lista obiektów `ConversationDto` (jak wyżej).

---

### POST /api/v1/message/messages
Opis: tworzy nową wiadomość w konwersacji.

Request (body):
```json
{
  "ConversationId": "<guid>",
  "Type": "Text | PriceOffer | PriceAccept | OfferApproval | Location",
  "Payload": { /* dowolny JSON, np. {"text":"..."} */ }
}
```

Response (201 Created) — body:
```json
{
  "messageId": "<guid>"
}
```
Błędy: 400/401/403/422

---

### GET /api/v1/message/conversations/{conversationId}/messages
Opis: pobiera listę wiadomości z danej konwersacji.

Query params (opcjonalne):
- `fromConversation` (int)
- `toConversation` (int)

Response (200 OK) — body: lista obiektów `MessageDto`:
```json
[
  {
    "MessageId": "<guid>",
    "ConversationId": "<guid>",
    "SenderId": "<guid>",
    "Type": "Text | PriceOffer | PriceAccept | OfferApproval | Location",
    "Payload": { /* JSON */ },
    "CreatedAt": "2026-05-24T12:01:00Z"
  },
  ...
]
```
Błędy: 400/401/404

---


### GET /api/v1/message/messages/{messageId}
Opis: pobiera jedną wiadomość po id.

Response (200 OK) — body: `MessageDto`:
```json
{
  "MessageId": "<guid>",
  "ConversationId": "<guid>",
  "SenderId": "<guid>",
  "Type": "Text | PriceOffer | PriceAccept | OfferApproval | Location",
  "Payload": { /* JSON */ },
  "CreatedAt": "2026-05-24T12:01:00Z"
}
```

Błędy: 404

---

### GET /api/v1/message/messages/sync
Opis: synchronizacja — pobiera wiadomości nowsze niż `lastReceivedAt`.

Query params (opcjonalne):
- `lastReceivedAt` (DateTime)

Response (200 OK) — body:
```json
{
  "messages": [ /* MessageDto array (jak wyżej) */ ],
  "serverTimestamp": "2026-05-24T12:10:00Z"
}
```

---

### POST /api/v1/message/messages/read
Opis: skonsolidowany read-receipt — potwierdza odczyt.

Request (body):
```json
{
  "ConversationId": "<guid>",
  // podać LastReadMessageId lub LastReadTimestamp
  "LastReadMessageId": "<guid>?",
  "LastReadTimestamp": "2026-05-24T12:05:00Z?"
}
```

Response (204 No Content) — brak body.

Błędy: 400/404/422

---

### GET /api/v1/message/users/{userId}
Opis: pobiera podstawowe dane użytkownika.

Response (200 OK) — body:
```json
{
  "id": "<guid>",
  "username": "string",
  "displayName": "string"
}
```
Błędy: 404

---

## Enumy
`MessageType`:
- `Text`
- `PriceOffer`
- `PriceAccept`
- `OfferApproval`
- `Location`

`ConversationType`:
- `Direct`
- `Group`
