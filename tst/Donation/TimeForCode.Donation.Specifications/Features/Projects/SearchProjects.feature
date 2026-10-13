Feature: Search active projects
	As a user
	I want to search and filter the active projects on the time for code platform
	So that I can quickly find open source work that matches my interests

Scenario: Searching without criteria lists active projects
	Given There are published projects on the time for code platform
	When The user searches the projects without criteria
	Then A paginated list of projects is returned
	And The total number of matching projects is included

Scenario: Searching does not require authentication
	Given There are published projects on the time for code platform
	When The user searches the projects without an account
	Then A paginated list of projects is returned

Scenario Outline: Projects that are not active are never listed
	Given There is a project that is <state>
	And There are published projects on the time for code platform
	When The user searches the projects without criteria
	Then The project that is <state> is not included in the results

	Examples:
		| state            |
		| in draft         |
		| pending approval |
		| archived         |

Scenario: User searches projects by text
	Given There are active projects with the names "Rocket Planner" and "Garden Tracker"
	When The user searches the projects for the text "rocket"
	Then Only the project "Rocket Planner" is included in the results

Scenario: User filters projects by language
	Given There are active projects written in "C#" and in "Rust"
	When The user filters the projects by the language "Rust"
	Then Only projects written in "Rust" are included in the results

Scenario: User filters projects by tag
	Given There are active projects tagged "accessibility" and "gaming"
	When The user filters the projects by the tag "gaming"
	Then Only projects tagged "gaming" are included in the results

Scenario: User combines text, language and tag criteria
	Given There are active projects that match only some of the text, language and tag criteria
	When The user searches the projects for the text "planner" with the language "C#" and the tag "gaming"
	Then Only projects matching all criteria are included in the results

Scenario: Search without matches returns an empty list
	Given There are published projects on the time for code platform
	When The user searches the projects for the text "no such project"
	Then An empty list of projects is returned
	And The total number of matching projects is zero

Scenario: User pages through the search results
	Given There are 5 active projects on the time for code platform
	When The user requests page 2 of the projects with 2 projects per page
	Then 2 projects are returned
	And The total number of matching projects is 5

Scenario: Search results have a consistent default order
	Given There are active projects created at different moments
	When The user searches the projects without criteria
	Then The projects are ordered from newest to oldest

Scenario: Projects with the same age keep a consistent order across pages
	Given There are active projects created at the same moment
	When The user searches the projects twice without criteria
	Then Both searches return the projects in the same order

Scenario Outline: User searches with invalid paging
	When The user requests page <page> of the projects with <size> projects per page
	Then The user is informed the paging request is invalid

	Examples:
		| page | size |
		| 0    | 10   |
		| -1   | 10   |
		| 1    | 0    |
		| 1    | -5   |
		| 1    | 1000 |
