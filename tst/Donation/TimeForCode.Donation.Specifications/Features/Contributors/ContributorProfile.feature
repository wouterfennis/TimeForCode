Feature: Contributor profile
	As a user
	I want to keep a profile with my role, skills and available hours
	So that donations can be fulfilled by the right people and my skills can be used for matchmaking

Scenario: User views their own contributor profile
	Given The user has an access token
	And The user is registered as a contributor with the role "Backend developer"
	When The user requests their own contributor profile
	Then The contributor profile shows:
		| Role              |
		| Backend developer |

Scenario: Contributor created by logging hours is reused as the profile
	Given The user has an access token
	And There is a published project on the time for code platform
	And The user has already logged 3 hours on the project
	When The user updates their contributor profile with:
		| Role              |
		| Backend developer |
	Then The contributor profile is updated on the time for code platform
	And The user is registered as one contributor on the time for code platform

Scenario: User updates their role, skills and available hours
	Given The user has an access token
	And The user is registered as a contributor on the time for code platform
	When The user updates their contributor profile with:
		| Role              | Skills      | Hours available |
		| Backend developer | C#, MongoDB | 10              |
	Then The contributor profile is updated on the time for code platform
	And The contributor profile shows:
		| Skills      | Hours available |
		| C#, MongoDB | 10              |

Scenario: User without a contributor profile creates one by updating it
	Given The user has an access token
	And The user is not yet a contributor on the time for code platform
	When The user updates their contributor profile with:
		| Role              |
		| Backend developer |
	Then The user is registered as a contributor on the time for code platform

Scenario: Skills are normalised
	Given The user has an access token
	And The user is registered as a contributor on the time for code platform
	When The user updates their contributor profile with:
		| Skills           |
		| C# , c#,MongoDB  |
	Then The contributor profile shows:
		| Skills      |
		| C#, MongoDB |

Scenario: Too many skills are rejected
	Given The user has an access token
	And The user is registered as a contributor on the time for code platform
	When The user updates their contributor profile with 51 skills
	Then The request is rejected as invalid
	And The contributor profile is unchanged

Scenario: A skill that is too long is rejected
	Given The user has an access token
	And The user is registered as a contributor on the time for code platform
	When The user updates their contributor profile with a skill of 101 characters
	Then The request is rejected as invalid

Scenario Outline: User attempts to set an invalid amount of available hours
	Given The user has an access token
	And The user is registered as a contributor on the time for code platform
	When The user updates their contributor profile with:
		| Hours available |
		| <hours>         |
	Then The request is rejected as invalid
	And The contributor profile is unchanged

	Examples:
		| hours |
		| -1    |
		| 10001 |

Scenario: User links their profile to a donor organization
	Given The user has an access token
	And The user is registered as a contributor on the time for code platform
	And There is a donor organization on the time for code platform
	When The user updates their contributor profile with:
		| Donor organization     |
		| the donor organization |
	Then The contributor profile is updated on the time for code platform
	And The contributor profile shows:
		| Donor organization     |
		| the donor organization |

Scenario: Linking to an unknown donor organization is rejected
	Given The user has an access token
	And The user is registered as a contributor on the time for code platform
	And There is no donor organization with the given identifier
	When The user updates their contributor profile with:
		| Donor organization     |
		| the donor organization |
	Then The request is rejected as not found
	And The contributor profile is unchanged

Scenario: Admin views the profile of another user
	Given The user has an administrator access token
	And Another user is registered as a contributor on the time for code platform
	When The user requests the contributor profile of the other user
	Then The contributor profile of the other user is returned

Scenario: Admin updates the profile of another user
	Given The user has an administrator access token
	And Another user is registered as a contributor on the time for code platform
	When The user updates the contributor profile of the other user with the role "Designer"
	Then The contributor profile of the other user shows the role "Designer"

Scenario: Admin views the contributors affiliated with a donor organization
	Given The user has an administrator access token
	And There is a donor organization on the time for code platform
	And Another user is registered as a contributor affiliated with the donor organization
	When The user requests the contributors of the donor organization
	Then The contributors of the donor organization are returned

Scenario: A regular user cannot view the profile of another user
	Given The user has an access token
	And Another user is registered as a contributor on the time for code platform
	When The user requests the contributor profile of the other user
	Then The request is rejected as forbidden

Scenario: A regular user cannot update the profile of another user
	Given The user has an access token
	And Another user is registered as a contributor on the time for code platform
	When The user updates the contributor profile of the other user with the role "Designer"
	Then The request is rejected as forbidden
	And The contributor profile of the other user is unchanged

Scenario: A regular user cannot view the contributors of a donor organization
	Given The user has an access token
	And There is a donor organization on the time for code platform
	When The user requests the contributors of the donor organization
	Then The request is rejected as forbidden

Scenario: Requesting a contributor profile that does not exist
	Given The user has an administrator access token
	And There is no contributor with the given identifier
	When The user requests the contributor profile of the other user
	Then The request is rejected as not found

@live
Scenario: Viewing a contributor profile requires authentication
	Given The user does not have an access token
	When The user requests their own contributor profile
	Then The request is rejected as unauthorized

@live
Scenario: Updating a contributor profile requires authentication
	Given The user does not have an access token
	When The user updates their contributor profile with:
		| Role              |
		| Backend developer |
	Then The request is rejected as unauthorized

@live
@liveonly
Scenario: An access token issued by the authorization service is accepted
	Given The user has an access token
	When The user updates their contributor profile with:
		| Role        |
		| Live tester |
	And The user requests their own contributor profile
	Then The contributor profile shows:
		| Role        |
		| Live tester |
