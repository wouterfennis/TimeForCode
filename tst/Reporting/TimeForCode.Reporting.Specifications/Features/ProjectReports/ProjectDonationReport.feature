Feature: Project donation report
	As a maintainer of a project
	I want to see the donations my project received, who logged hours and how far we have progressed
	So that I can follow the support my project gets and thank its contributors

Scenario: Maintainer views the detailed report of their project
	Given The user has an access token
	And The user is the maintainer of the project
	And The project has a donation of 10 hours with status "active"
	And Contributor "alice" has logged 4 hours on the project
	When The user requests the donation report of the project
	Then The report shows 10 hours received and 4 hours logged
	And The donation report shows a progress of 40 percent

Scenario: Detailed report lists each contributor with their hours
	Given The user has an access token
	And The user is the maintainer of the project
	And The project has a donation of 10 hours with status "active"
	And Contributor "alice" has logged 3 hours on the project
	And Contributor "bob" has logged 2 hours on the project
	When The user requests the donation report of the project
	Then The donation report lists contributor "alice" with 3 hours
	And The donation report lists contributor "bob" with 2 hours

Scenario: Detailed report shows the donation statuses and only counts active and completed hours as received
	Given The user has an access token
	And The user is the maintainer of the project
	And The project has a donation of 10 hours with status "active"
	And The project has a donation of 5 hours with status "pending"
	And The project has a donation of 8 hours with status "cancelled"
	When The user requests the donation report of the project
	Then The report shows 10 hours received and 0 hours logged
	And The donation report shows 1 active donation
	And The donation report shows 1 pending donation
	And The donation report shows 1 cancelled donation

Scenario: Detailed report of a project without donations is empty
	Given The user has an access token
	And The user is the maintainer of the project
	When The user requests the donation report of the project
	Then The report shows 0 hours received and 0 hours logged
	And The donation report shows a progress of 0 percent

Scenario: Administrator views the detailed report of any project
	Given The user has an administrator access token
	And There is a project on the time for code platform
	When The user requests the donation report of the project
	Then The report shows 0 hours received and 0 hours logged

@live
Scenario: Other user cannot view the detailed report of a project
	Given The user has an access token
	And There is a project on the time for code platform
	When The user requests the donation report of the project
	Then The request is rejected as forbidden

@live
Scenario: Detailed report requires an account
	Given The user does not have an access token
	And There is a project on the time for code platform
	When The user requests the donation report of the project
	Then The request is rejected as unauthorized

@live
Scenario: Detailed report of an unknown project is not found
	Given The user has an access token
	And There is no project with the given identifier
	When The user requests the donation report of the project
	Then The request is rejected as not found

Scenario: Anyone views the public summary of a project without an account
	Given The user does not have an access token
	And There is a project on the time for code platform
	And The project has a donation of 10 hours with status "active"
	And Contributor "alice" has logged 4 hours on the project
	When The user requests the public summary of the project
	Then The report shows 10 hours received and 4 hours logged
	And The public summary shows 1 donor and 1 contributor

Scenario: Public summary exposes no personal data
	Given The user does not have an access token
	And There is a project on the time for code platform
	And Contributor "alice" has logged 3 hours on the project
	When The user requests the public summary of the project
	Then The public summary does not mention contributor "alice"

@live
Scenario: Public summary of an unknown project is not found
	Given The user does not have an access token
	And There is no project with the given identifier
	When The user requests the public summary of the project
	Then The request is rejected as not found
