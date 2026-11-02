<div align="center">
  <h1>skydrop</h1>
  <h3>Azure hosted file sharing service.</h3>
  <h5>Free to use for everyone. Simple fast file sharing.</h5>
</div>

## Usage

1. Head over to https://skydrop.skybounce.io and upload a file.
2. Once uploaded, you'll be take to the download page and given a link to copy.
3. This link can be used by anyone to download your uploaded file until it expires.

## Design Choices

**Proxy downloads > SAS links** <br />
Although shared access signatures are faster and don't clog up API compute, the fact they're a bearer token makes capping egress charges (the main cost of the service) too complex, requiring multiple monitoring and panic button services to ensure I'm not overcharged. The solution, route all streaming through the API, and enforce a strict monthly usage limit.

**Atomic Usage Reservation** <br />
Before an upload or download can begin, it must first reserve the usage in bytes. Atomic conditional patch operations in Azure CosmosDb ensures parallel calls from different API instances don't cause an overflow in usage.

**Stateless API** <br />
It would have been easier to track usage in a singleton service running on the API, but this would cause issues if/when the webapp is scaled horizontally.

## API

| Method | Route | Description |
|---|---|---|
| `POST` | `/File` | Upload a file as the raw request body. Requires `Content-Length`, `Content-Type: application/octet-stream`, and the URL-encoded name in `X-File-Name`. Returns the file ID. |
| `GET` | `/File/Metadata/{fileId}` | File metadata as JSON: name, size and download count. |
| `GET` | `/File/Download/{fileId}` | The file itself, as an attachment. Supports range requests for resumable downloads. |

| Status | Meaning |
|---|---|
| `404` | Unknown or expired file |
| `411` | Missing `Content-Length` |
| `413` | File exceeds the 5 GB limit |
| `429` | Monthly upload or download quota reached |
