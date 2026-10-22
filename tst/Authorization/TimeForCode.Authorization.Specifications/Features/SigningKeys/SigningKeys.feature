Feature: Signing key hygiene
	As an operator of the time for code platform
	I want tokens to be signed with identifiable, rotatable keys
	So that a restart or a key rotation does not log out users or break token validation

Scenario: Platform refuses to start without a signing certificate outside Development
	Given The time for code platform is configured without a signing certificate
	And The time for code platform runs outside the Development environment
	When The time for code platform starts
	Then The time for code platform fails to start
	And The startup failure states that a signing certificate is required

Scenario: Platform starts without a signing certificate in Development
	Given The time for code platform is configured without a signing certificate
	And The time for code platform runs in the Development environment
	When The time for code platform starts
	Then The time for code platform starts successfully

Scenario: Issued access token identifies its signing key
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	And The external platform calls the time for code platform to complete the authorization
	When The user requests the published signing keys
	Then The issued access token names a signing key
	And The published signing keys contain the key named by the access token

@live
Scenario: Published signing keys describe their purpose and algorithm
	When The user requests the published signing keys
	Then Each published signing key has a key identifier
	And Each published signing key is marked for signature use
	And Each published signing key states its signing algorithm

Scenario: Token signed with the previous key remains valid during rotation
	Given The time for code platform has a current and a previous signing key
	And The user holds an access token signed with the previous signing key
	When The user requests the published signing keys
	Then Both the current and the previous signing key are published
	And The access token signed with the previous signing key is accepted

Scenario: Token signed with the current key is valid during rotation
	Given The time for code platform has a current and a previous signing key
	And The user holds an access token signed with the current signing key
	When The user requests the published signing keys
	Then The access token signed with the current signing key is accepted
