Feature: Logout
    As a logged-in user
    I want to log out
    So that I am returned to an unauthenticated state

Scenario: Logged-in user logs out and is redirected to the home page in an unauthenticated state
    Given The website is running at "http://localhost:8083"
    And The identity provider mock is running at "http://localhost:8081"
    And The user is logged in
    When The visitor triggers logout
    Then The visitor is returned to the home page
    And The login link is visible
    And The logout link is not visible
