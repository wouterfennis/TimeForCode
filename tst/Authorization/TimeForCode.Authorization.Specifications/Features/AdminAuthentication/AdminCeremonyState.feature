Feature: Admin passkey ceremony state
	As the time for code platform running on several instances
	I want a passkey ceremony to continue on any instance and to work only once
	So that the admin can sign in regardless of which instance answers and a captured ceremony cannot be replayed

Scenario: A ceremony started on one instance is completed on another
	Given The time for code platform already has a registered admin credential
	And The user started an admin login on one instance
	When The user completes the admin login on a different instance
	Then The admin login succeeds

Scenario: A ceremony that is unknown, expired or already used is rejected
	Given The time for code platform already has a registered admin credential
	When The user completes an admin login with a ceremony that is unknown, expired or already used
	Then The admin login is rejected as invalid

Scenario: A ceremony cannot be completed twice
	Given The time for code platform already has a registered admin credential
	When The user completes the admin login twice with the same ceremony
	Then The admin login succeeds
	And The second admin login is rejected as invalid
