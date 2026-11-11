Feature: Contributor badges
	As a contributor
	I want to be awarded a badge when I cross an hour milestone
	So that my effort is recognised and I can share it publicly

Background:
	Given There is a project on the time for code platform

Scenario: Contributor earns a badge when reaching a threshold
	Given There is a contributor badge tier "Bronze" at 10 hours
	And Contributor "Alice" has logged 10 hours on the project
	When The user requests the badges of Contributor "Alice"
	Then Contributor "Alice" has the badge "Bronze"

Scenario: Contributor below the threshold has no badge
	Given There is a contributor badge tier "Bronze" at 10 hours
	And Contributor "Alice" has logged 9 hours on the project
	When The user requests the badges of Contributor "Alice"
	Then Contributor "Alice" has no badges

Scenario: Contributor crossing several thresholds earns every tier
	Given There is a contributor badge tier "Bronze" at 10 hours
	And There is a contributor badge tier "Silver" at 20 hours
	And Contributor "Alice" has logged 25 hours on the project
	When The user requests the badges of Contributor "Alice"
	Then Contributor "Alice" has the badge "Bronze"
	And Contributor "Alice" has the badge "Silver"

Scenario: Hours across several projects add up
	Given There is a contributor badge tier "Bronze" at 10 hours
	And Contributor "Alice" has logged 5 hours on each of 2 projects
	When The user requests the badges of Contributor "Alice"
	Then Contributor "Alice" has the badge "Bronze"

Scenario: A badge is awarded only once per tier
	Given There is a contributor badge tier "Bronze" at 10 hours
	And Contributor "Alice" has logged 12 hours on the project
	And The user has requested the badges of Contributor "Alice"
	And Contributor "Alice" has logged 5 hours on the project
	When The user requests the badges of Contributor "Alice"
	Then Contributor "Alice" has the badge "Bronze" exactly once

Scenario: Badge requests at the same time award a badge only once
	Given There is a contributor badge tier "Bronze" at 10 hours
	And Contributor "Alice" has logged 12 hours on the project
	When The badges of Contributor "Alice" are requested by 5 visitors at the same time
	Then Contributor "Alice" has the badge "Bronze" exactly once

Scenario: Organization tiers do not award contributor badges
	Given There is an organization badge tier "Bronze" at 10 hours
	And Contributor "Alice" has logged 20 hours on the project
	When The user requests the badges of Contributor "Alice"
	Then Contributor "Alice" has no badges

Scenario: Contributor badges are public and expose no personal data
	Given There is a contributor badge tier "Bronze" at 10 hours
	And Contributor "Alice" has logged 10 hours on the project
	And The user does not have an access token
	When The user requests the badges of Contributor "Alice"
	Then Contributor "Alice" has the badge "Bronze"
	And The response contains only badge names and award dates

Scenario: Badges of an unknown contributor are not found
	Given There is no contributor with the given identifier
	When The user requests the badges of Contributor "Alice"
	Then The request is rejected as not found
