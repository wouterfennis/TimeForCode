Feature: Logout
	As a user
	I want to logout from the time for code platform
	So that my session can no longer be used for authentication

Scenario: User has not logged in at the time for code platform
	Given The user has not logged in at the time for code platform
	When The user logs out from the external platform
	Then The logout is confirmed

Scenario: User has logged in at the time for code platform
	Given The user has an access token
	And The user has a refresh token
	When The user logs out from the external platform
	Then The refresh token is revoked
	Then The access token is revoked
