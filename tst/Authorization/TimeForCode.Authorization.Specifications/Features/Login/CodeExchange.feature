Feature: Code exchange
	As a user
	I want to sign in without any token passing through my browser
	So that a script in my browser cannot steal my session

After the external platform has confirmed the user, the time for code platform redirects the browser back
with a single-use code. The client that started the login exchanges that code, together with the secret
it created at the start of the login, for the tokens.

Background:
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform

Scenario: The browser is redirected back with a one-time code and no token
	When The external platform calls the time for code platform to complete the authorization
	Then The user is redirected back with a one-time code
	And The one-time code is not a token
	And No cookie is set for the user

Scenario: The client that started the login exchanges the code for tokens
	Given The external platform calls the time for code platform to complete the authorization
	When The user exchanges the one-time code
	Then An access token is returned
	And A refresh token is returned
	And The access token contains the user scope
	And No cookie is set for the user

Scenario: The refresh token from an exchange is not kept in readable form
	Given The external platform calls the time for code platform to complete the authorization
	When The user exchanges the one-time code
	Then The refresh token is not stored in readable form

Scenario: First-time user's profile is created on login
	Given The user does not yet have a profile on the time for code platform
	And The external platform calls the time for code platform to complete the authorization
	When The user exchanges the one-time code
	Then The time for code platform notifies the user that their profile has been created

Scenario: Returning user's profile is updated on login
	Given The user already has a profile on the time for code platform
	And The external platform calls the time for code platform to complete the authorization
	When The user exchanges the one-time code
	Then The time for code platform does not notify the user of a new profile

Scenario: A code can be exchanged only once
	Given The external platform calls the time for code platform to complete the authorization
	And The one-time code has already been used
	When The user exchanges the one-time code
	Then The exchange is refused
	And The request is rejected as invalid

Scenario: A code from another browser is useless without its secret
	Given The external platform calls the time for code platform to complete the authorization
	When The user exchanges the one-time code with a verifier of another login
	Then The exchange is refused
	And The request is rejected as invalid

Scenario: A code that was refused for a wrong secret is consumed
	Given The external platform calls the time for code platform to complete the authorization
	And The user exchanges the one-time code with a verifier of another login
	When The user exchanges the one-time code
	Then The exchange is refused

Scenario: A code expires after a short time
	Given The external platform calls the time for code platform to complete the authorization
	And More than a minute has passed since the sign-in was completed
	When The user exchanges the one-time code
	Then The exchange is refused
	And The request is rejected as invalid

Scenario: A code that was never issued is refused
	When The user exchanges a one-time code that was never issued
	Then The exchange is refused
	And The request is rejected as invalid

Scenario: A secret of the wrong shape is rejected
	Given The external platform calls the time for code platform to complete the authorization
	When The user exchanges the one-time code with a verifier that is too short
	Then The request is rejected as invalid
