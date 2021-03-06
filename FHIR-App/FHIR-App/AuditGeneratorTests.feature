Feature: AuditGenerator

Scenario: Hello World
	Given The Event is a simple one
	Then The Test should pass


Scenario: Log File Testing
	Given We add a line to the Log File
	Then The Log file should exist
	And The Log file should have a line in it

Scenario: Single Blank Audit Event
	Given An AuditEvent exists
	When The AuditEvents are recived
	Then The Log file should exist
	And The Log file should have a line in it

Scenario: Making a filter
	Given An AuditEvent exists
	And The AuditEvent has a Type of Testing
	And A Filter exists
	And The Filter checks a display of Testing
	And The Filter paths to resource/type
	And The Filter will Hide matches
	And An AuditEvent exists
	And The AuditEvent has a Type of Passes
	When The AuditEvents are recived
	Then The Log file should exist
	And The Log file should have exactly 1 line in it