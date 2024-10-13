Feature: Callback
	As a user
	I want to login with my existing account from another platform
	So that my data is shared between the platforms

Login consists technically on two parts.
1. The initial call to the authorization service where a redirect url towards the external platform is returned.
2. The user is redirected to the external platform to perform the authentication.
3. The external platform calls the time for code platform to complete the authorization
4. The time for code platform calls the external platform to receive the access token

Scenario: User has not logged in at the external platform
	Given The user has not logged in at the external platform
	When The external platform calls the time for code platform to complete the authorization
	Then A authentication token is not returned
	And The following error message is returned: "State is not known"

Scenario: User has logged in at the external platform
	Given The user logs in at the external platform
	When The external platform calls the time for code platform to complete the authorization
	Then A authentication token is returned
	And The user information is saved in the time for code platform
