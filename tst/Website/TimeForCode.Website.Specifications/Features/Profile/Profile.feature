Feature: Profile
    As a logged-in user
    I want to view my profile
    So that I can see my GitHub login handle

Scenario: Logged-in user sees their GitHub login handle on the profile page
    Given The website is running at "http://localhost:8083"
    And The identity provider mock is running at "http://localhost:8081"
    And The user is logged in
    When The visitor navigates to the profile page
    Then The login handle is visible
