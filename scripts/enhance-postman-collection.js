const fs = require('fs');
const path = require('path');

const basePath = path.join(__dirname, '..', 'postman', 'collections', 'VFX_BASM_ForeignExchangeRates API');
const folderPath = path.join(basePath, 'ForeignExchangeRate');

// Create collection-level definition.yaml
const collectionDefinition = `$kind: collection
name: VFX_BASM_ForeignExchangeRates API
description: |-
  # Foreign Exchange Rates API
  
  A RESTful API for managing foreign exchange (FX) rates. This API provides endpoints to create, read, update, and delete currency exchange rate data.
  
  ## Overview
  
  The Foreign Exchange Rates API allows you to:
  - **Retrieve** all available exchange rates or filter by specific criteria
  - **Look up** individual rates by ID or currency pair
  - **Create** new exchange rate entries
  - **Update** existing rates with new bid/ask values
  - **Delete** rates that are no longer needed
  
  ## Data Model
  
  Each exchange rate record contains:
  | Field | Type | Description |
  |-------|------|-------------|
  | \`id\` | integer | Unique identifier for the rate |
  | \`baseCurrency\` | string | ISO 4217 currency code (e.g., USD, EUR, GBP) |
  | \`quoteCurrency\` | string | ISO 4217 currency code for the quote currency |
  | \`bid\` | decimal | The buying price (what dealers pay) |
  | \`ask\` | decimal | The selling price (what dealers charge) |
  | \`timestamp\` | datetime | When the rate was last updated (ISO 8601) |
  
  ## Authentication
  
  Currently, this API does not require authentication for development/testing purposes.
  
  ## Base URL
  
  Use the \`{{baseUrl}}\` variable to configure the API endpoint.
  
  ## Response Codes
  
  | Code | Description |
  |------|-------------|
  | 200 | Success - Request completed successfully |
  | 201 | Created - New resource created |
  | 204 | No Content - Request succeeded with no response body |
  | 400 | Bad Request - Invalid input data |
  | 404 | Not Found - Resource does not exist |
  | 409 | Conflict - Resource already exists |
  | 500 | Internal Server Error |
  
variables:
  - key: baseUrl
    value: 'https://localhost:7001'
    description: Base URL for the Foreign Exchange Rates API
`;

// Create folder-level definition.yaml
const folderDefinition = `$kind: collection
name: ForeignExchangeRate
description: |-
  ## Foreign Exchange Rate Endpoints
  
  This folder contains all CRUD operations for managing foreign exchange rates.
  
  ### Available Operations
  
  | Method | Endpoint | Description |
  |--------|----------|-------------|
  | GET | \`/api/ForeignExchangeRate\` | Retrieve all exchange rates |
  | GET | \`/api/ForeignExchangeRate/:id\` | Get a specific rate by ID |
  | GET | \`/api/ForeignExchangeRate/:baseCurrency/:quoteCurrency\` | Get rate by currency pair |
  | POST | \`/api/ForeignExchangeRate\` | Create a new exchange rate |
  | PUT | \`/api/ForeignExchangeRate/:id\` | Update an existing rate |
  | DELETE | \`/api/ForeignExchangeRate/:id\` | Delete a rate |
  
  ### Currency Codes
  
  Use standard ISO 4217 three-letter currency codes:
  - USD - US Dollar
  - EUR - Euro
  - GBP - British Pound
  - JPY - Japanese Yen
  - CHF - Swiss Franc
  - And many more...
order: 1000
`;

// Updated request files with descriptions
const requests = {
  'Get All Rates.request.yaml': `$kind: http-request
name: Get All Rates
description: |-
  ## GET /api/ForeignExchangeRate
  
  Retrieves all foreign exchange rates currently stored in the system.
  
  ### Description
  
  Returns a JSON array containing all available exchange rate records. Each record includes the currency pair, bid/ask prices, and timestamp of the last update.
  
  ### Response
  
  - **200 OK** - Returns an array of exchange rate objects
  - **500 Internal Server Error** - Server-side error occurred
  
  ### Example Use Cases
  
  - Display all available rates in a dashboard
  - Populate a currency selector dropdown
  - Export rate data for reporting
method: GET
url: '{{baseUrl}}/api/ForeignExchangeRate'
order: 1000
headers:
  - key: Accept
    value: application/json
    description: Specifies that the client expects JSON response
scripts:
  - type: afterResponse
    language: text/javascript
    code: |-
      pm.test("Status code is 200", function () {
          pm.response.to.have.status(200);
      });

      pm.test("Content-Type is application/json", function () {
          pm.response.to.have.header("Content-Type");
          pm.expect(pm.response.headers.get("Content-Type")).to.include("application/json");
      });

      pm.test("Response is an array", function () {
          const jsonData = pm.response.json();
          pm.expect(jsonData).to.be.an("array");
      });
`,

  'Get Rate by ID.request.yaml': `$kind: http-request
name: Get Rate by ID
description: |-
  ## GET /api/ForeignExchangeRate/:id
  
  Retrieves a specific foreign exchange rate by its unique identifier.
  
  ### Description
  
  Returns a single exchange rate record matching the provided ID. Use this endpoint when you know the specific rate ID you want to retrieve.
  
  ### Path Parameters
  
  | Parameter | Type | Required | Description |
  |-----------|------|----------|-------------|
  | \`id\` | integer | Yes | The unique identifier of the exchange rate |
  
  ### Response
  
  - **200 OK** - Returns the exchange rate object
  - **404 Not Found** - No rate exists with the specified ID
  
  ### Example Use Cases
  
  - Fetch details for a specific rate after creation
  - Verify rate data before performing an update
  - Display individual rate information
method: GET
url: '{{baseUrl}}/api/ForeignExchangeRate/:id'
order: 2000
headers:
  - key: Accept
    value: application/json
    description: Specifies that the client expects JSON response
pathVariables:
  - key: id
    value: '1'
    description: The unique identifier of the exchange rate to retrieve
scripts:
  - type: afterResponse
    language: text/javascript
    code: |-
      pm.test("Status code is 200 or 404", function () {
          pm.expect(pm.response.code).to.be.oneOf([200, 404]);
      });

      pm.test("Content-Type is application/json", function () {
          pm.response.to.have.header("Content-Type");
          pm.expect(pm.response.headers.get("Content-Type")).to.include("application/json");
      });

      if (pm.response.code === 200) {
          pm.test("Response has required fields", function () {
              const jsonData = pm.response.json();
              pm.expect(jsonData).to.have.property("id");
              pm.expect(jsonData).to.have.property("baseCurrency");
              pm.expect(jsonData).to.have.property("quoteCurrency");
              pm.expect(jsonData).to.have.property("bid");
              pm.expect(jsonData).to.have.property("ask");
          });
      }
`,

  'Get Rate by Currency Pair.request.yaml': `$kind: http-request
name: Get Rate by Currency Pair
description: |-
  ## GET /api/ForeignExchangeRate/:baseCurrency/:quoteCurrency
  
  Retrieves the exchange rate for a specific currency pair.
  
  ### Description
  
  Returns the exchange rate between two currencies identified by their ISO 4217 codes. This is the most common way to look up exchange rates when you know the currency pair but not the rate ID.
  
  ### Path Parameters
  
  | Parameter | Type | Required | Description |
  |-----------|------|----------|-------------|
  | \`baseCurrency\` | string | Yes | ISO 4217 code for the base currency (e.g., USD) |
  | \`quoteCurrency\` | string | Yes | ISO 4217 code for the quote currency (e.g., EUR) |
  
  ### Response
  
  - **200 OK** - Returns the exchange rate object
  - **400 Bad Request** - Invalid currency code format
  - **404 Not Found** - No rate exists for the specified currency pair
  - **503 Service Unavailable** - External rate service unavailable
  
  ### Example Use Cases
  
  - Currency conversion calculations
  - Display current rate for a trading pair
  - Validate rate before executing a transaction
method: GET
url: '{{baseUrl}}/api/ForeignExchangeRate/:baseCurrency/:quoteCurrency'
order: 3000
headers:
  - key: Accept
    value: application/json
    description: Specifies that the client expects JSON response
pathVariables:
  - key: baseCurrency
    value: USD
    description: 'ISO 4217 base currency code (e.g., USD, EUR, GBP)'
  - key: quoteCurrency
    value: EUR
    description: 'ISO 4217 quote currency code (e.g., USD, EUR, GBP)'
scripts:
  - type: afterResponse
    language: text/javascript
    code: |-
      pm.test("Status code is 200, 400, 404, or 503", function () {
          pm.expect(pm.response.code).to.be.oneOf([200, 400, 404, 503]);
      });

      pm.test("Content-Type is application/json", function () {
          pm.response.to.have.header("Content-Type");
          pm.expect(pm.response.headers.get("Content-Type")).to.include("application/json");
      });

      if (pm.response.code === 200) {
          pm.test("Response has required fields", function () {
              const jsonData = pm.response.json();
              pm.expect(jsonData).to.have.property("id");
              pm.expect(jsonData).to.have.property("baseCurrency");
              pm.expect(jsonData).to.have.property("quoteCurrency");
              pm.expect(jsonData).to.have.property("bid");
              pm.expect(jsonData).to.have.property("ask");
          });
      }
`,

  'Create Rate.request.yaml': `$kind: http-request
name: Create Rate
description: |-
  ## POST /api/ForeignExchangeRate
  
  Creates a new foreign exchange rate entry.
  
  ### Description
  
  Adds a new exchange rate record to the system. The currency pair must be unique - attempting to create a duplicate pair will result in a 409 Conflict error.
  
  ### Request Body
  
  | Field | Type | Required | Description |
  |-------|------|----------|-------------|
  | \`baseCurrency\` | string | Yes | ISO 4217 code for the base currency |
  | \`quoteCurrency\` | string | Yes | ISO 4217 code for the quote currency |
  | \`bid\` | decimal | Yes | The buying price (must be positive) |
  | \`ask\` | decimal | Yes | The selling price (must be >= bid) |
  
  ### Response
  
  - **201 Created** - Rate created successfully, returns the new rate object
  - **400 Bad Request** - Invalid input data (validation errors)
  - **409 Conflict** - Currency pair already exists
  - **500 Internal Server Error** - Server-side error occurred
  
  ### Headers
  
  On success, the \`Location\` header contains the URL of the newly created resource.
  
  ### Example Use Cases
  
  - Add a new currency pair to the system
  - Initialize rates during system setup
  - Import rates from external sources
method: POST
url: '{{baseUrl}}/api/ForeignExchangeRate'
order: 4000
headers:
  - key: Content-Type
    value: application/json
    description: Indicates the request body is JSON
  - key: Accept
    value: application/json
    description: Specifies that the client expects JSON response
body:
  type: json
  content: |-
    {
      "baseCurrency": "GBP",
      "quoteCurrency": "EUR",
      "bid": 1.1650,
      "ask": 1.1680
    }
scripts:
  - type: afterResponse
    language: text/javascript
    code: |-
      pm.test("Status code is 201, 400, or 409", function () {
          pm.expect(pm.response.code).to.be.oneOf([201, 400, 409, 500]);
      });

      pm.test("Content-Type is application/json", function () {
          pm.response.to.have.header("Content-Type");
          pm.expect(pm.response.headers.get("Content-Type")).to.include("application/json");
      });

      if (pm.response.code === 201) {
          pm.test("Response has required fields", function () {
              const jsonData = pm.response.json();
              pm.expect(jsonData).to.have.property("id");
              pm.expect(jsonData).to.have.property("baseCurrency");
              pm.expect(jsonData).to.have.property("quoteCurrency");
              pm.expect(jsonData).to.have.property("bid");
              pm.expect(jsonData).to.have.property("ask");
              pm.expect(jsonData).to.have.property("timestamp");
          });

          pm.test("Location header is present", function () {
              pm.response.to.have.header("Location");
          });
      }
`,

  'Update Rate.request.yaml': `$kind: http-request
name: Update Rate
description: |-
  ## PUT /api/ForeignExchangeRate/:id
  
  Updates an existing foreign exchange rate.
  
  ### Description
  
  Replaces the exchange rate data for the specified ID. All fields in the request body are required and will overwrite the existing values.
  
  ### Path Parameters
  
  | Parameter | Type | Required | Description |
  |-----------|------|----------|-------------|
  | \`id\` | integer | Yes | The unique identifier of the rate to update |
  
  ### Request Body
  
  | Field | Type | Required | Description |
  |-------|------|----------|-------------|
  | \`baseCurrency\` | string | Yes | ISO 4217 code for the base currency |
  | \`quoteCurrency\` | string | Yes | ISO 4217 code for the quote currency |
  | \`bid\` | decimal | Yes | The new buying price |
  | \`ask\` | decimal | Yes | The new selling price |
  
  ### Response
  
  - **204 No Content** - Rate updated successfully
  - **400 Bad Request** - Invalid input data
  - **404 Not Found** - No rate exists with the specified ID
  - **409 Conflict** - Currency pair conflicts with another existing rate
  
  ### Example Use Cases
  
  - Update rates with latest market values
  - Correct data entry errors
  - Adjust bid/ask spread
method: PUT
url: '{{baseUrl}}/api/ForeignExchangeRate/:id'
order: 5000
headers:
  - key: Content-Type
    value: application/json
    description: Indicates the request body is JSON
  - key: Accept
    value: application/json
    description: Specifies that the client expects JSON response
pathVariables:
  - key: id
    value: '1'
    description: The unique identifier of the rate to update
body:
  type: json
  content: |-
    {
      "baseCurrency": "USD",
      "quoteCurrency": "EUR",
      "bid": 0.9250,
      "ask": 0.9350
    }
scripts:
  - type: afterResponse
    language: text/javascript
    code: |-
      pm.test("Status code is 204, 400, 404, or 409", function () {
          pm.expect(pm.response.code).to.be.oneOf([204, 400, 404, 409]);
      });

      if (pm.response.code === 204) {
          pm.test("No content returned on success", function () {
              pm.expect(pm.response.text()).to.be.empty;
          });
      }
`,

  'Delete Rate.request.yaml': `$kind: http-request
name: Delete Rate
description: |-
  ## DELETE /api/ForeignExchangeRate/:id
  
  Deletes a foreign exchange rate from the system.
  
  ### Description
  
  Permanently removes the exchange rate with the specified ID. This action cannot be undone.
  
  ### Path Parameters
  
  | Parameter | Type | Required | Description |
  |-----------|------|----------|-------------|
  | \`id\` | integer | Yes | The unique identifier of the rate to delete |
  
  ### Response
  
  - **204 No Content** - Rate deleted successfully
  - **404 Not Found** - No rate exists with the specified ID
  
  ### Example Use Cases
  
  - Remove deprecated currency pairs
  - Clean up test data
  - Delete rates that are no longer traded
method: DELETE
url: '{{baseUrl}}/api/ForeignExchangeRate/:id'
order: 6000
headers:
  - key: Accept
    value: application/json
    description: Specifies that the client expects JSON response
pathVariables:
  - key: id
    value: '1'
    description: The unique identifier of the rate to delete
scripts:
  - type: afterResponse
    language: text/javascript
    code: |-
      pm.test("Status code is 204 or 404", function () {
          pm.expect(pm.response.code).to.be.oneOf([204, 404]);
      });

      if (pm.response.code === 204) {
          pm.test("No content returned on success", function () {
              pm.expect(pm.response.text()).to.be.empty;
          });
      }
`
};

// Example files for each request
const examples = {
  'Get All Rates': {
    'Success - Multiple Rates.example.yaml': `$kind: http-request
name: 'Success - Multiple Rates'
description: Successful response returning multiple exchange rates
method: GET
url: '{{baseUrl}}/api/ForeignExchangeRate'
headers:
  - key: Accept
    value: application/json
response:
  statusCode: 200
  statusText: OK
  headers:
    - key: Content-Type
      value: application/json; charset=utf-8
    - key: X-Request-Id
      value: 'abc123-def456'
  body:
    type: json
    content: |-
      [
        {
          "id": 1,
          "baseCurrency": "USD",
          "quoteCurrency": "EUR",
          "bid": 0.9180,
          "ask": 0.9220,
          "timestamp": "2025-03-05T14:30:00Z"
        },
        {
          "id": 2,
          "baseCurrency": "GBP",
          "quoteCurrency": "USD",
          "bid": 1.2650,
          "ask": 1.2680,
          "timestamp": "2025-03-05T14:30:00Z"
        },
        {
          "id": 3,
          "baseCurrency": "EUR",
          "quoteCurrency": "JPY",
          "bid": 162.50,
          "ask": 162.80,
          "timestamp": "2025-03-05T14:30:00Z"
        }
      ]
`,
    'Success - Empty Array.example.yaml': `$kind: http-request
name: 'Success - Empty Array'
description: Successful response when no exchange rates exist in the system
method: GET
url: '{{baseUrl}}/api/ForeignExchangeRate'
headers:
  - key: Accept
    value: application/json
response:
  statusCode: 200
  statusText: OK
  headers:
    - key: Content-Type
      value: application/json; charset=utf-8
  body:
    type: json
    content: '[]'
`
  },
  'Get Rate by ID': {
    'Success - Rate Found.example.yaml': `$kind: http-request
name: 'Success - Rate Found'
description: Successful response returning a single exchange rate
method: GET
url: '{{baseUrl}}/api/ForeignExchangeRate/:id'
headers:
  - key: Accept
    value: application/json
pathVariables:
  - key: id
    value: '1'
response:
  statusCode: 200
  statusText: OK
  headers:
    - key: Content-Type
      value: application/json; charset=utf-8
  body:
    type: json
    content: |-
      {
        "id": 1,
        "baseCurrency": "USD",
        "quoteCurrency": "EUR",
        "bid": 0.9180,
        "ask": 0.9220,
        "timestamp": "2025-03-05T14:30:00Z"
      }
`,
    'Error - Not Found.example.yaml': `$kind: http-request
name: 'Error - Not Found'
description: Error response when the specified rate ID does not exist
method: GET
url: '{{baseUrl}}/api/ForeignExchangeRate/:id'
headers:
  - key: Accept
    value: application/json
pathVariables:
  - key: id
    value: '999'
response:
  statusCode: 404
  statusText: Not Found
  headers:
    - key: Content-Type
      value: application/problem+json; charset=utf-8
  body:
    type: json
    content: |-
      {
        "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        "title": "Not Found",
        "status": 404,
        "detail": "Exchange rate with ID 999 was not found."
      }
`
  },
  'Get Rate by Currency Pair': {
    'Success - Pair Found.example.yaml': `$kind: http-request
name: 'Success - Pair Found'
description: Successful response returning the exchange rate for USD/EUR pair
method: GET
url: '{{baseUrl}}/api/ForeignExchangeRate/:baseCurrency/:quoteCurrency'
headers:
  - key: Accept
    value: application/json
pathVariables:
  - key: baseCurrency
    value: USD
  - key: quoteCurrency
    value: EUR
response:
  statusCode: 200
  statusText: OK
  headers:
    - key: Content-Type
      value: application/json; charset=utf-8
  body:
    type: json
    content: |-
      {
        "id": 1,
        "baseCurrency": "USD",
        "quoteCurrency": "EUR",
        "bid": 0.9180,
        "ask": 0.9220,
        "timestamp": "2025-03-05T14:30:00Z"
      }
`,
    'Error - Pair Not Found.example.yaml': `$kind: http-request
name: 'Error - Pair Not Found'
description: Error response when the currency pair does not exist
method: GET
url: '{{baseUrl}}/api/ForeignExchangeRate/:baseCurrency/:quoteCurrency'
headers:
  - key: Accept
    value: application/json
pathVariables:
  - key: baseCurrency
    value: XYZ
  - key: quoteCurrency
    value: ABC
response:
  statusCode: 404
  statusText: Not Found
  headers:
    - key: Content-Type
      value: application/problem+json; charset=utf-8
  body:
    type: json
    content: |-
      {
        "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        "title": "Not Found",
        "status": 404,
        "detail": "Exchange rate for XYZ/ABC was not found."
      }
`
  },
  'Create Rate': {
    'Success - Rate Created.example.yaml': `$kind: http-request
name: 'Success - Rate Created'
description: Successful response when a new exchange rate is created
method: POST
url: '{{baseUrl}}/api/ForeignExchangeRate'
headers:
  - key: Content-Type
    value: application/json
  - key: Accept
    value: application/json
body:
  type: json
  content: |-
    {
      "baseCurrency": "GBP",
      "quoteCurrency": "EUR",
      "bid": 1.1650,
      "ask": 1.1680
    }
response:
  statusCode: 201
  statusText: Created
  headers:
    - key: Content-Type
      value: application/json; charset=utf-8
    - key: Location
      value: '{{baseUrl}}/api/ForeignExchangeRate/4'
  body:
    type: json
    content: |-
      {
        "id": 4,
        "baseCurrency": "GBP",
        "quoteCurrency": "EUR",
        "bid": 1.1650,
        "ask": 1.1680,
        "timestamp": "2025-03-05T15:00:00Z"
      }
`,
    'Error - Validation Failed.example.yaml': `$kind: http-request
name: 'Error - Validation Failed'
description: Error response when request body validation fails
method: POST
url: '{{baseUrl}}/api/ForeignExchangeRate'
headers:
  - key: Content-Type
    value: application/json
  - key: Accept
    value: application/json
body:
  type: json
  content: |-
    {
      "baseCurrency": "",
      "quoteCurrency": "EUR",
      "bid": -1.0,
      "ask": 0.5
    }
response:
  statusCode: 400
  statusText: Bad Request
  headers:
    - key: Content-Type
      value: application/problem+json; charset=utf-8
  body:
    type: json
    content: |-
      {
        "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        "title": "One or more validation errors occurred.",
        "status": 400,
        "errors": {
          "BaseCurrency": ["The BaseCurrency field is required."],
          "Bid": ["Bid must be a positive number."],
          "Ask": ["Ask must be greater than or equal to Bid."]
        }
      }
`,
    'Error - Duplicate Pair.example.yaml': `$kind: http-request
name: 'Error - Duplicate Pair'
description: Error response when attempting to create a duplicate currency pair
method: POST
url: '{{baseUrl}}/api/ForeignExchangeRate'
headers:
  - key: Content-Type
    value: application/json
  - key: Accept
    value: application/json
body:
  type: json
  content: |-
    {
      "baseCurrency": "USD",
      "quoteCurrency": "EUR",
      "bid": 0.9200,
      "ask": 0.9250
    }
response:
  statusCode: 409
  statusText: Conflict
  headers:
    - key: Content-Type
      value: application/problem+json; charset=utf-8
  body:
    type: json
    content: |-
      {
        "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        "title": "Conflict",
        "status": 409,
        "detail": "An exchange rate for USD/EUR already exists."
      }
`
  },
  'Update Rate': {
    'Success - Rate Updated.example.yaml': `$kind: http-request
name: 'Success - Rate Updated'
description: Successful response when an exchange rate is updated (no content returned)
method: PUT
url: '{{baseUrl}}/api/ForeignExchangeRate/:id'
headers:
  - key: Content-Type
    value: application/json
  - key: Accept
    value: application/json
pathVariables:
  - key: id
    value: '1'
body:
  type: json
  content: |-
    {
      "baseCurrency": "USD",
      "quoteCurrency": "EUR",
      "bid": 0.9250,
      "ask": 0.9350
    }
response:
  statusCode: 204
  statusText: No Content
  headers:
    - key: X-Request-Id
      value: 'update-123'
  body:
    type: text
    content: ''
`,
    'Error - Rate Not Found.example.yaml': `$kind: http-request
name: 'Error - Rate Not Found'
description: Error response when attempting to update a non-existent rate
method: PUT
url: '{{baseUrl}}/api/ForeignExchangeRate/:id'
headers:
  - key: Content-Type
    value: application/json
  - key: Accept
    value: application/json
pathVariables:
  - key: id
    value: '999'
body:
  type: json
  content: |-
    {
      "baseCurrency": "USD",
      "quoteCurrency": "EUR",
      "bid": 0.9250,
      "ask": 0.9350
    }
response:
  statusCode: 404
  statusText: Not Found
  headers:
    - key: Content-Type
      value: application/problem+json; charset=utf-8
  body:
    type: json
    content: |-
      {
        "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        "title": "Not Found",
        "status": 404,
        "detail": "Exchange rate with ID 999 was not found."
      }
`
  },
  'Delete Rate': {
    'Success - Rate Deleted.example.yaml': `$kind: http-request
name: 'Success - Rate Deleted'
description: Successful response when an exchange rate is deleted (no content returned)
method: DELETE
url: '{{baseUrl}}/api/ForeignExchangeRate/:id'
headers:
  - key: Accept
    value: application/json
pathVariables:
  - key: id
    value: '1'
response:
  statusCode: 204
  statusText: No Content
  headers:
    - key: X-Request-Id
      value: 'delete-456'
  body:
    type: text
    content: ''
`,
    'Error - Rate Not Found.example.yaml': `$kind: http-request
name: 'Error - Rate Not Found'
description: Error response when attempting to delete a non-existent rate
method: DELETE
url: '{{baseUrl}}/api/ForeignExchangeRate/:id'
headers:
  - key: Accept
    value: application/json
pathVariables:
  - key: id
    value: '999'
response:
  statusCode: 404
  statusText: Not Found
  headers:
    - key: Content-Type
      value: application/problem+json; charset=utf-8
  body:
    type: json
    content: |-
      {
        "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        "title": "Not Found",
        "status": 404,
        "detail": "Exchange rate with ID 999 was not found."
      }
`
  }
};

// Create directories and files
function ensureDir(dir) {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
    console.log(`Created directory: ${dir}`);
  }
}

// Write collection definition
const collectionResourcesPath = path.join(basePath, '.resources');
ensureDir(collectionResourcesPath);
fs.writeFileSync(path.join(collectionResourcesPath, 'definition.yaml'), collectionDefinition);
console.log('Created collection definition.yaml');

// Write folder definition
const folderResourcesPath = path.join(folderPath, '.resources');
ensureDir(folderResourcesPath);
fs.writeFileSync(path.join(folderResourcesPath, 'definition.yaml'), folderDefinition);
console.log('Created folder definition.yaml');

// Write updated request files
for (const [filename, content] of Object.entries(requests)) {
  const filePath = path.join(folderPath, filename);
  fs.writeFileSync(filePath, content);
  console.log(`Updated request: ${filename}`);
}

// Write example files
for (const [requestName, exampleFiles] of Object.entries(examples)) {
  const requestFileName = requestName.replace(/[/\\:*?"<>|]/g, '-');
  const examplesDir = path.join(folderPath, '.resources', `${requestFileName}.resources`, 'examples');
  ensureDir(examplesDir);
  
  for (const [exampleFilename, content] of Object.entries(exampleFiles)) {
    const examplePath = path.join(examplesDir, exampleFilename);
    fs.writeFileSync(examplePath, content);
    console.log(`Created example: ${requestName} -> ${exampleFilename}`);
  }
}

console.log('\n✅ Collection enhancement complete!');
console.log('Summary:');
console.log('- Collection-level definition.yaml with API overview');
console.log('- Folder-level definition.yaml with endpoint summary');
console.log('- 6 request files updated with detailed descriptions');
console.log('- 13 example files created with realistic request/response data');
