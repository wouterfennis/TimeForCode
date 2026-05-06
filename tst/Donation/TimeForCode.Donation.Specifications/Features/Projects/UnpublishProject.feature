Feature: Unpublish a project
	As a user
	I want to remove a previously published repository from the time for code platform
	So that it no longer appears in the public project listing

Scenario: Authenticated user unpublishes a project
	Given The user has an access token
	And The user has already published the repository on the time for code platform
	When The user unpublishes the project on the time for code platform
	Then The project is archived on the time for code platform
	And The project no longer appears in the public project listing

Scenario: Unpublishing a project requires authentication
	Given The user does not have an access token
	When The user unpublishes the project on the time for code platform
	Then The user is informed they must be logged in
