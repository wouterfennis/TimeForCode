Feature: Admin passkey ceremony state
	As the time for code platform running on several instances
	I want a passkey ceremony to continue on any instance
	So that the admin can sign in regardless of which instance answers

Scenario: A ceremony started on one instance is completed on another
	Given The time for code platform already has a registered admin credential
	And The user started an admin login on one instance
	When The user completes the admin login on a different instance
	Then The admin login succeeds

Scenario: A ceremony state that cannot be read is rejected
	Given The time for code platform already has a registered admin credential
	When The user completes an admin login with a ceremony state that cannot be read
	Then The admin login is rejected as invalid
