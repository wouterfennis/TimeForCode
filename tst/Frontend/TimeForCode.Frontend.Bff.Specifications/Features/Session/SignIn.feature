Feature: Sign in through the website
	As a user
	I want to sign in with GitHub on the time for code website and sign out again
	So that I can use the parts of the platform that need my account without my tokens ever reaching the website's scripts

The website runs on its own address and forwards to the platform's services.
Sign-in, the session check and sign-out all go through the website's address,
so the token cookies belong to the site the user is on.
Scenarios tagged @live need the compose stack (TIMEFORCODE_LIVE_FRONTEND_URL) and are skipped without it.

@live
Scenario: Signing in sends the user to the external platform through the website
	Given The user has an account at the external platform
	When The user signs in on the time for code website
	Then The user is redirected to the external platform
	And The external platform is asked to return the user to the time for code website

@live
Scenario: The user completes the sign-in and returns to the website signed in
	Given The user has an account at the external platform
	When The user completes the sign-in on the time for code website
	Then The user is back on the time for code website
	And The time for code website learns that the user is signed in
	And The token cookies cannot be read by the website's scripts
	And No token appears in the address or in any answer the website reads

Scenario Outline: The website learns that nobody is signed in
	Given The user has <session>
	When The time for code website checks who is signed in
	Then The time for code website learns that nobody is signed in

	Examples:
		| session                  |
		| no access token          |
		| an expired access token  |
		| a malformed access token |

Scenario: The website learns who the signed-in user is
	Given The user has an access token
	When The time for code website checks who is signed in
	Then The time for code website learns the login and avatar of the user
	And The answer contains no token

Scenario: The website learns that an administrator is signed in
	Given The user has an administrator access token
	When The time for code website checks who is signed in
	Then The time for code website learns that the user is an administrator
	And The answer contains no token

@live
Scenario: A signed-in user requests their profile through the website
	Given The user has signed in on the time for code website
	When The user requests their profile through the time for code website
	Then The profile is returned with name, login, avatar, email, company, bio, and location

@live
Scenario: Signing out ends the session
	Given The user has signed in on the time for code website
	When The user signs out on the time for code website
	Then The refresh token is revoked
	And The user has no token cookies left
	And The time for code website learns that nobody is signed in
