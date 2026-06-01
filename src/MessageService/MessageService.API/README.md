SignalR Hub Quick Start

This file explains how to connect to the SignalR hub from a browser client and send messages.

Hub URL: /hub/chat

Client (TypeScript) example using @microsoft/signalr:

```ts
import * as signalR from '@microsoft/signalr';

const jwt = localStorage.getItem('jwt') || '';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://localhost:5001/hub/chat', {
    accessTokenFactory: () => jwt
  })
  .withAutomaticReconnect()
  .build();

connection.on('MessageCreated', (payload) => {
  console.log('MessageCreated', payload);
});

await connection.start();

// join conversation (conversationId is a GUID as string)
await connection.invoke('JoinConversation', conversationId);

// send message (CreateMessageDto shape)
const dto = { type: 'TEXT', payload: { text: 'hello' } };
const res = await connection.invoke('SendMessage', conversationId, dto);
console.log('sent', res);

```

Notes:
- The server expects a JWT token either in the Authorization header for HTTP calls or as the `access_token` query parameter for SignalR transports. The client example above uses `accessTokenFactory` which SignalR will attach.
- The server broadcasts `MessageCreated` and `MessagesRead` events. Subscribe to them as shown above.

