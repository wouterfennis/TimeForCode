Feature: Manage the lifecycle of a donation
	As a donor or an administrator
	I want to activate and cancel donations
	So that pledged hours only count while the commitment is in force

Scenario: Donor activates a pending donation
	Given The user has an access token
	And The user has a pending donation
	When The user activates the donation
	Then The donation is active

Scenario: Administrator activates a pending donation
	Given The user has an administrator access token
	And There is a pending donation of another user
	When The user activates the donation
	Then The donation is active

Scenario: Donor cancels a pending donation
	Given The user has an access token
	And The user has a pending donation
	When The user cancels the donation
	Then The donation is cancelled

Scenario: Donor cancels an active donation
	Given The user has an access token
	And The user has an active donation
	When The user cancels the donation
	Then The donation is cancelled

Scenario: Administrator cancels an active donation
	Given The user has an administrator access token
	And There is an active donation of another user
	When The user cancels the donation
	Then The donation is cancelled

Scenario Outline: Other users cannot change a donation
	Given The user has an access token
	And There is a <state> donation of another user
	When The user <action> the donation
	Then The request is rejected as forbidden

	Examples:
		| state   | action    |
		| pending | activates |
		| pending | cancels   |
		| active  | cancels   |

Scenario Outline: Transitions that are not allowed are rejected
	Given The user has an access token
	And The user has a <state> donation
	When The user <action> the donation
	Then The request is rejected as a conflict

	Examples:
		| state     | action    |
		| active    | activates |
		| completed | activates |
		| completed | cancels   |
		| cancelled | activates |
		| cancelled | cancels   |

Scenario: Donation completes automatically when all pledged hours are logged
	Given The user has an active donation of 5 hours
	And The user has an access token
	When The user logs 5 hours on the project
	Then The donation is completed
	And The donation shows 5 hours logged

Scenario: Donation stays active while hours remain
	Given The user has an active donation of 5 hours
	And The user has an access token
	When The user logs 3 hours on the project
	Then The donation is active
	And The donation shows 3 hours logged

Scenario: Hours logged on a pending donation do not count
	Given The user has a pending donation of 5 hours
	And The user has an access token
	When The user logs 5 hours on the project
	Then The donation is pending
	And The donation shows 0 hours logged

Scenario: Activating or cancelling an unknown donation is rejected
	Given The user has an access token
	And There is no donation with the given identifier
	When The user activates the donation
	Then The request is rejected as not found

Scenario: Donor organization hours stay consistent with its donations
	Given The user has an access token
	And The donor organization has an active donation of 10 hours
	When The user logs 4 hours on the project
	Then The donor organization shows 4 hours spent
	And The donor organization shows 6 hours available
