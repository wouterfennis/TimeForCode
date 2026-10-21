Feature: Test-only admin access
	As a tester of the time for code platform
	I want to obtain an admin token on a local or test stack without a passkey
	So that admin actions can be exercised in live tests without weakening production

Scenario: Test admin access is not available by default
	Given The time for code platform runs in the Development environment
	And The time for code platform has test admin access disabled
	When The time for code platform starts
	And The user requests a test admin token with the test admin secret
	Then The test admin token is not available

Scenario: Test admin token is issued when enabled
	Given The time for code platform runs in the Development environment
	And The time for code platform has test admin access enabled
	When The time for code platform starts
	And The user requests a test admin token with the test admin secret
	Then The test admin token grants the admin scope

Scenario: Test admin token is refused with a wrong secret
	Given The time for code platform runs in the Development environment
	And The time for code platform has test admin access enabled
	When The time for code platform starts
	And The user requests a test admin token with a wrong secret
	Then The test admin token is refused

Scenario Outline: Platform refuses to start with test admin access enabled outside local and test environments
	Given The time for code platform runs in the <environment> environment
	And The time for code platform has test admin access enabled
	When The time for code platform starts
	Then The time for code platform fails to start
	And The startup failure states that test admin access is not allowed

Examples:
	| environment |
	| Production  |
	| Staging     |

Scenario: Platform refuses to start with test admin access enabled without a secret
	Given The time for code platform runs in the Development environment
	And The time for code platform has test admin access enabled without a secret
	When The time for code platform starts
	Then The time for code platform fails to start
	And The startup failure states that a test admin secret is required
