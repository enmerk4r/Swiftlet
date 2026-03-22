# Test Results

**6** passed, **0** failed, **0** errors (6 total)

## auth.gh — PASSED

*6 tests, 0.5ms*

| Status | Test Name | Message | Expected | Actual | Time(ms) |
|--------|-----------|---------|----------|--------|----------|
| PASS | api-key\header-key-name |  | some-key | some-key | 0.4 |
| PASS | api-key\header-value |  | some-value | some-value | 0.0 |
| PASS | api-key\query-param-key-name |  | some-key | some-key | 0.0 |
| PASS | api-key\query-param-value |  | some-value | some-value | 0.0 |
| PASS | api-key\header-not-null |  |  | {     "isValid": true,     "typeName": "Http Header",     "typeDescription": "A header for an HTTP request",     "value": {       "key": "some-key",       "value": "some-value"     },     "isValidWhyNot": ""   } | 0.1 |
| PASS | api-key\query-param-not-null |  |  | {     "isValid": true,     "typeName": "Query Param",     "typeDescription": "A query parameter for an HTTP request",     "value": {       "key": "some-key",       "value": "some-value"     },     "isValidWhyNot": ""   } | 0.0 |