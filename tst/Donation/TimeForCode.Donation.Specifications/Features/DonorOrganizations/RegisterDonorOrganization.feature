Feature: Register a donor organization
	As a user
	I want to register a donor organization on the time for code platform
	So that the organization's donated developer time can be tracked

Scenario: User registers a new donor organization
	When The user registers a donor organization with a valid name
	Then The donor organization is registered on the time for code platform

Scenario: Registering a donor organization with a duplicate name is rejected
	Given There is already a donor organization with the same name on the time for code platform
	When The user registers a donor organization with a valid name
	Then The request is rejected as a conflict

@live
Scenario: Registering a donor organization without a name is rejected
	When The user registers a donor organization with:
		| Name |
		|      |
	Then The request is rejected as invalid

Scenario: Registering a donor organization with a blank name is rejected
	When The user registers a donor organization with a blank name
	Then The request is rejected as invalid

Scenario: User registers a donor organization with contact details
	When The user registers a donor organization with:
		| Contact email            | Website              |
		| open-source@acme.example | https://acme.example |
	Then The donor organization is registered on the time for code platform

Scenario Outline: Registering a donor organization with invalid contact details is rejected
	When The user registers a donor organization with:
		| Contact email | Website   |
		| <email>       | <website> |
	Then The request is rejected as invalid

	Examples:
		| email                    | website                  |
		| not-an-email             | https://acme.example     |
		|                          | https://acme.example     |
		| open-source@acme.example | mailto:info@acme.example |
		| open-source@acme.example | javascript:alert(1)      |
		| open-source@acme.example | not a url                |
		| open-source@acme.example |                          |

@live
@liveonly
Scenario: A registered donor organization can be retrieved
	When The user registers a donor organization with a unique name
	Then The donor organization can be retrieved on the live platform