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
	And The AuditEvent has a Subtype of SubTest
	And A Filter exists
	And The Filter checks a display of Testing
	And The Filter paths to resource/type
	And The Filter will Hide matches
	And An AuditEvent exists
	And The AuditEvent has a Type of Passes
	And The AuditEvent has a Subtype of SubTest
	When The AuditEvents are recived
	Then The Log file should exist
	And The Log file should have exactly 1 line in it
	And Log file line number 1 should be "Passes (SubTest)"

Scenario: Testing NOT
	Given An AuditEvent exists
	And The AuditEvent has a Type of Testing
	And The AuditEvent has a Subtype of SubTest
	And A Filter exists
	And The Filter checks a display of Testing
	And The Filter negates it's input
	And The Filter paths to resource/type
	And The Filter will Hide matches
	And An AuditEvent exists
	And The AuditEvent has a Type of Passes
	And The AuditEvent has a Subtype of SubTest
	When The AuditEvents are recived
	Then The Log file should exist
	And The Log file should have exactly 1 line in it
	And Log file line number 1 should be "Testing (SubTest)"

Scenario: Testing OR
	Given An AuditEvent exists
	And The AuditEvent has a Type of Testing
	And The AuditEvent has a Subtype of SubTest
	And An AuditEvent exists
	And The AuditEvent has a Type of Fails
	And The AuditEvent has a Subtype of SubTest
	And An AuditEvent exists
	And The AuditEvent has a Type of Passes
	And The AuditEvent has a Subtype of SubTest
	And A Filter exists
	And The Filter checks a display of Testing
	And The Filter checks a display of Fails
	And The Filter requires either to be true
	And The Filter paths to resource/type
	And The Filter will Hide matches
	When The AuditEvents are recived
	Then The Log file should exist
	And The Log file should have exactly 1 line in it
	And Log file line number 1 should be "Passes (SubTest)"

Scenario: Testing AND
	Given An AuditEvent exists
	And The AuditEvent has a Type of Testing
	And The AuditEvent has a Subtype of SubTest
	And The AuditEvent has an ID of Passes
	And An AuditEvent exists
	And The AuditEvent has a Type of Testing
	And The AuditEvent has a Subtype of SubTest
	And The AuditEvent has an ID of Fails
	And A Filter exists
	And The Filter checks a display of Testing
	And The Filter paths to resource/type
	And The Filter checks a id of Fails
	And The Filter paths to resource
	And The Filter requires both to be true
	And The Filter will Hide matches
	When The AuditEvents are recived
	Then The Log file should exist
	And The Log file should have exactly 1 line in it
	And Log file line number 1 should be "Testing (SubTest)"

Scenario: Testing Type1
	Given An AuditEvent exists
	And The AuditEvent has a Type of Testing
	And The AuditEvent has a Subtype of SubTest
	And A Filter exists
	And The Filter checks the Type is Testing
	And The Filter will Hide matches
	And An AuditEvent exists
	And The AuditEvent has a Type of Passes
	And The AuditEvent has a Subtype of SubTest
	When The AuditEvents are recived
	Then The Log file should exist
	And The Log file should have exactly 1 line in it
	And Log file line number 1 should be "Passes (SubTest)"
	
Scenario: Testing Type2
	Given An AuditEvent exists
	And The AuditEvent has a Type of Testing
	And The AuditEvent has a Subtype of SubTest
	And A Filter exists
	And The Filter checks the Types are Testing/SubTest
	And The Filter will Hide matches
	And An AuditEvent exists
	And The AuditEvent has a Type of Passes
	And The AuditEvent has a Subtype of SubTest
	When The AuditEvents are recived
	Then The Log file should exist
	And The Log file should have exactly 1 line in it
	And Log file line number 1 should be "Passes (SubTest)"

Scenario: Testing Exists
	Given An AuditEvent exists
	And The AuditEvent has a Type of Testing
	And The AuditEvent has a Subtype of SubTest
	And An AuditEvent exists
	And The AuditEvent has a Type of Passes
	And A Filter exists
	And The Filter checks that subtype Exists
	And The Filter paths to resource
	And The Filter will Hide matches
	When The AuditEvents are recived
	Then The Log file should exist
	And The Log file should have exactly 1 line in it
	And Log file line number 1 should be "Passes ()"

Scenario: Testing Sending Event to remote
	Given An AuditEvent exists
	And The AuditEvent has a Type of Testing
	And The AuditEvent has a Subtype of SubTest
	And The remote server is http://wildfhir4.aegis.net/fhir4-0-1/
	When The system sends the audit events to the remote server
	Then The remote server should have a matching audit event
	
Scenario: Testing Sending Events to all Remotes
	Given An AuditEvent exists
	And The AuditEvent has a Type of Testing
	And The AuditEvent has a Subtype of SubTest
	And The remote server is <Server>
	When The system sends the audit events to the remote server
	Then The remote server should have a matching audit event

	Examples: 
	| Server                                             |
	| https://server.subscriptions.argo.run/r4/          |
	| https://api.logicahealth.org/covidigtest/open/     |
	| http://gic-sandbox.alphora.com/cqf-ruler-r4/fhir/  |
	| http://hapi.fhir.org/baseR4/                       |
	| https://wildfhir4.aegis.net/fhir4-0-1/             |
	| http://34.94.253.50:8080/hapi-fhir-jpaserver/fhir/ |