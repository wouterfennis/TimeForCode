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

Scenario: User has logs out with invalid redirect url
	Given The user has an access token
	And The user has a refresh token
	When The user logs out from the external platform with invalid redirect url
	Then The user is informed the logout redirect uri is rejected
