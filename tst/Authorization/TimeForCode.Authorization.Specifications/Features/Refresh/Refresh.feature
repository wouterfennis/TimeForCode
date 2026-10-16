Feature: Refresh token
	As a user
	I want to refresh my authentication token
	So that I won't have to log in again

Scenario: User has not logged in at the external platform
	Given The user has not logged in at the time for code platform
	When The user calls the refresh token endpoint
	Then The following refresh error message is returned: "No refresh token found."

Scenario: User has expired refresh token
	Given The user has an expired refresh token
	When The user calls the refresh token endpoint
	Then The following refresh error message is returned: "Token refresh failed."

Scenario: User has valid refresh token
	Given The user has a refresh token
	When The user calls the refresh token endpoint
	Then An access token is returned
	And A refresh token is returned

Scenario: The refresh token is not kept in readable form
	Given The user has a refresh token
	When The user calls the refresh token endpoint
	Then The refresh token is not stored in readable form

Scenario: A rotated refresh token is replayed
	Given The user has a refresh token
	And The user has already refreshed with that refresh token
	When The user calls the refresh token endpoint
	Then The following refresh error message is returned: "Token refresh failed."
	And All other refresh tokens of the user are revoked

Scenario: A refresh is requested with a read-only request
	Given The user has a refresh token
	When The user calls the refresh token endpoint with a read-only request
	Then The request is not allowed

Scenario: The admin refreshes the session
	Given The user has an admin session token
	And The user has a refresh token
	When The user calls the refresh token endpoint
	Then An access token is returned
	And The access token contains the admin role and the admin scope

Scenario: A regular user refreshes the session
	Given The user has a refresh token
	When The user calls the refresh token endpoint
	Then An access token is returned
	And The access token contains the user scope
