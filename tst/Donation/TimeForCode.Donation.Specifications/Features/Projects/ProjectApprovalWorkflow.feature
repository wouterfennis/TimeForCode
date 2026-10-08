Feature: Project submission and admin approval workflow
	As a maintainer
	I want to submit my project for review and have an administrator approve it
	So that only vetted projects appear publicly

Scenario: Maintainer submits a draft project for review
	Given The user has an access token
	And The user has a project in draft
	When The user submits the project for review
	Then The project is pending approval

Scenario: Administrator approves a pending project
	Given The user has an administrator access token
	And There is a project pending approval
	When The administrator approves the project
	Then The project is active

Scenario: Administrator requests changes on a pending project
	Given The user has an administrator access token
	And There is a project pending approval
	When The administrator requests changes with the reason "Please add a description"
	Then The project is back in draft
	And The project shows the reviewer reason "Please add a description"

Scenario: Maintainer archives an active project
	Given The user has an access token
	And The user has an active project
	When The user archives the project
	Then The project is archived

Scenario: Maintainer re-activates an archived project
	Given The user has an access token
	And The user has an archived project
	When The user re-activates the project
	Then The project is active

Scenario Outline: Invalid lifecycle transitions are rejected
	Given The user has an administrator access token
	And There is a project that is <state>
	When The administrator approves the project
	Then The user is informed the transition is not allowed

	Examples:
		| state    |
		| in draft |
		| active   |
		| archived |

Scenario: Non-administrator cannot approve a project
	Given The user has an access token
	And There is a project pending approval
	When The user approves the project
	Then The user is informed they do not have permission

Scenario: Non-administrator cannot request changes
	Given The user has an access token
	And There is a project pending approval
	When The user requests changes with the reason "Please add a description"
	Then The user is informed they do not have permission

Scenario: Anonymous caller cannot approve a project
	Given The user does not have an access token
	When The user approves the project
	Then The user is informed they must be logged in

Scenario Outline: Only the maintainer can change their project
	Given The user has an access token
	And There is a project published by another user
	When The user <action> the project
	Then The user is informed they do not have permission

	Examples:
		| action             |
		| submits for review |
		| archives           |
		| re-activates       |

Scenario: Only active projects can receive donations
	Given There is a project that is in draft
	When A donor donates to the project
	Then The user is informed the project cannot receive donations
