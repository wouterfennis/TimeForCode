Feature: Login
	As a user
	I want to login with my existing account from another platform
	So that my data is shared between the platforms

Login consists technically on two parts.
1. The initial call to the authorization service where a redirect url towards the external platform is returned.
2. The user is redirected to the external platform to perform the authentication.
3. The external platform calls the time for code platform to complete the authorization
4. The time for code platform calls the external platform to receive the access token

Scenario: User wants to login with an existing account
	Given The user has an account at the external platform
	When The user logs in at the time for code platform
	Then The user is redirected to the external platform