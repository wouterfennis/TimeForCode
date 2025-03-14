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
	Then The following refresh error message is returned: "Refresh token expired"

Scenario: User has valid refresh token
	Given The user has a refresh token
	When The user calls the refresh token endpoint
	Then An access token is returned
	And A refresh token is returned

