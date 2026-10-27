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
	When The user searches the projects with:
		| Text   |
		| rocket |
	Then Only the project "Rocket Planner" is included in the results

Scenario Outline: User filters projects by a single criterion
	Given There are active projects whose <criterion> is "<other>" and "<value>"
	When The user searches the projects with:
		| <column>  |
		| <value>   |
	Then Only projects with the <criterion> "<value>" are included in the results

	Examples:
		| criterion | column   | other         | value  |
		| language  | Language | C#            | Rust   |
		| tag       | Tag      | accessibility | gaming |

Scenario: User combines text, language and tag criteria
	Given There are active projects that match only some of the text, language and tag criteria
	When The user searches the projects with:
		| Text    | Language | Tag    |
		| planner | C#       | gaming |
	Then Only projects matching all criteria are included in the results

Scenario: Search without matches returns an empty list
	Given There are published projects on the time for code platform
	When The user searches the projects with:
		| Text            |
		| no such project |
	Then An empty list of projects is returned
	And The total number of matching projects is zero

Scenario: User pages through the search results
	Given There are 5 active projects on the time for code platform
	When The user searches the projects with:
		| Page | Page size |
		| 2    | 2         |
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
	When The user searches the projects with:
		| Page   | Page size |
		| <page> | <size>    |
	Then The request is rejected as invalid

	Examples:
		| page       | size |
		| 0          | 10   |
		| -1         | 10   |
		| 1          | 0    |
		| 1          | -5   |
		| 1          | 1000 |
		| 10001      | 10   |
		| 2147483647 | 100  |

Scenario Outline: User searches with criteria that are too long
	When The user searches the projects with a <criterion> of 101 characters
	Then The request is rejected as invalid

	Examples:
		| criterion |
		| text      |
		| language  |
		| tag       |

@live
@liveonly
Scenario: An approved project is found by searching for its name
	Given The user has an access token
	And The user has an active project registered on the live platform
	When The user searches the projects for the name of the project
	Then Only the project of the user is included in the results on the live platform

@live
@liveonly
Scenario: A project in draft is not found by searching for its name
	Given The user has an access token
	And The user has published a new repository on the live platform
	When The user searches the projects for the name of the project
	Then An empty list of projects is returned