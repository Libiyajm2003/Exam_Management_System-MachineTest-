import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { IStudent } from '../../models/IStudent';
import { ISubject } from '../../models/ISubject';
import { IExamMaster } from '../../models/IExamMaster';
import { StudentService} from '../../services/student';
import { SubjectService } from '../../services/subject';
import { ExamMasterService } from '../../services/exam-master';


@Component({
selector: 'app-exam-form',
imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink],
templateUrl: './exam-form.html',
})
export class ExamFormComponent implements OnInit {
form: FormGroup;
students: IStudent[] = [];
subjects: ISubject[] = [];
filteredStudents: IStudent[] = [];
filteredSubjects: ISubject[] = [];
activeSubjectRowIndex: number | null = null;
showAddStudentModal: boolean = false;
newStudent: Partial<IStudent> = { studentName: '', mail: '' };


constructor(
private fb: FormBuilder,
private studentSvc: StudentService,
private subjectSvc: SubjectService,
private examMasterSvc: ExamMasterService,
private router: Router
) {
this.form = this.fb.group({
studentID: [null, Validators.required],
studentName: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(250)]],
examYear: [new Date().getFullYear(), [Validators.required, Validators.min(2000), Validators.max(2100)]],
details: this.fb.array([])
});
}


ngOnInit(): void {
this.loadLookups();
this.addDetailRow();
}


get details() { return this.form.get('details') as FormArray; }

get totalMark(): number {
const details = this.details.value;
return details.reduce((sum: number, d: any) => sum + (d.marks || 0), 0);
}

get passOrFail(): string {
const details = this.details.value;
if (details.length === 0) return 'FAIL';
// All subjects must have marks >= 25 to pass
const allPassed = details.every((d: any) => (d.marks || 0) >= 25);
return allPassed ? 'PASS' : 'FAIL';
}


loadLookups() {
this.studentSvc.getAll().subscribe({
next: (s) => { 
this.students = s; 
this.filteredStudents = s; 
},
error: (err) => {
console.error('Error loading students:', err);
}
});
this.subjectSvc.getAll().subscribe({
next: (s) => { 
this.subjects = s; 
this.filteredSubjects = s; 
},
error: (err) => {
console.error('Error loading subjects:', err);
}
});
}


addDetailRow() {
this.details.push(this.fb.group({
subjectID: [null, Validators.required],
subjectName: [''],
marks: [0, [Validators.required, Validators.min(0), Validators.max(100)]]
}));
}

addSubjectToTable() {
this.addDetailRow();
}

removeDetailRow(index: number) {
if (this.details.length > 1) {
this.details.removeAt(index);
}
}

onStudentInput(value: string) {
// If user is typing, filter the list
if (!value || value.length < 1) {
this.filteredStudents = this.students;
// Clear studentID when input is cleared
this.form.patchValue({ studentID: null });
return;
}
const search = value.toLowerCase();
this.filteredStudents = this.students.filter(s => 
s.studentName.toLowerCase().includes(search) || 
s.mail.toLowerCase().includes(search)
);
// Clear studentID if user is typing (not selecting from list)
if (this.filteredStudents.length > 0) {
this.form.patchValue({ studentID: null });
}
}

onStudentFocus() {
// Show all students when input is focused
if (this.students.length > 0) {
this.filteredStudents = this.students;
}
}

@HostListener('document:click', ['$event'])
onDocumentClick(event: MouseEvent) {
const target = event.target as HTMLElement;
// Close dropdown if clicking outside the student input area
if (!target.closest('.student-autocomplete-container')) {
this.filteredStudents = [];
}
}

openAddStudentModal() {
this.showAddStudentModal = true;
this.newStudent = { studentName: '', mail: '' };
}

closeAddStudentModal() {
this.showAddStudentModal = false;
this.newStudent = { studentName: '', mail: '' };
}

addNewStudent() {
if (!this.newStudent.studentName || this.newStudent.studentName.length < 5) {
alert('Student name must be at least 5 characters long');
return;
}
if (!this.newStudent.mail || !this.newStudent.mail.includes('@')) {
alert('Please enter a valid email address');
return;
}

this.studentSvc.create(this.newStudent).subscribe({
next: (created) => {
alert('Student added successfully!');
this.students.push(created);
this.filteredStudents = this.students;
this.selectStudent(created);
this.closeAddStudentModal();
},
error: (err) => {
let errorMessage = 'Error adding student';
if (err.error) {
if (typeof err.error === 'string') {
errorMessage = err.error;
} else if (err.error.title) {
errorMessage = err.error.title;
} else if (err.error.message) {
errorMessage = err.error.message;
}
}
alert(errorMessage);
console.error('Error adding student:', err);
}
});
}

selectStudent(student: IStudent) {
this.form.patchValue({
studentID: student.studentID,
studentName: `${student.studentName} (${student.mail})`
});
this.filteredStudents = [];
// Mark the field as touched to show validation
this.form.get('studentID')?.markAsTouched();
this.form.get('studentName')?.markAsTouched();
}

onSubjectInput(value: string, index: number) {
this.activeSubjectRowIndex = index;
if (!value || value.length < 2) {
this.filteredSubjects = this.subjects;
return;
}
const search = value.toLowerCase();
this.filteredSubjects = this.subjects.filter(s => 
s.subjectName.toLowerCase().includes(search)
);
}

selectSubject(subject: ISubject, index: number) {
const detailGroup = this.details.at(index) as FormGroup;
detailGroup.patchValue({
subjectID: subject.subjectID,
subjectName: subject.subjectName
});
this.filteredSubjects = [];
this.activeSubjectRowIndex = null;
}

save() {
if (this.form.invalid) {
this.form.markAllAsTouched();
return;
}

const formValue = this.form.value;

// Validate that student is selected
if (!formValue.studentID) {
alert('Please select a student');
return;
}

// Validate that all details have subject and marks
const invalidDetails = formValue.details.some((d: any) => !d.subjectID || d.marks === null || d.marks === undefined);
if (invalidDetails) {
alert('Please fill all subject and marks fields');
return;
}

// Check for duplicate subjects
const subjectIds = formValue.details.map((d: any) => d.subjectID);
const uniqueSubjectIds = [...new Set(subjectIds)];
if (subjectIds.length !== uniqueSubjectIds.length) {
alert('Duplicate subjects are not allowed. Each subject can only be added once.');
return;
}

// Prepare request with master and details together
const request = {
studentID: formValue.studentID,
examYear: formValue.examYear,
details: formValue.details.map((d: any) => ({
subjectID: d.subjectID,
marks: parseFloat(d.marks) || 0
}))
};

this.examMasterSvc.create(request).subscribe({
next: (created) => {
alert('Exam saved successfully!');
this.form.reset();
this.details.clear();
this.addDetailRow();
this.router.navigate(['/list']);
},
error: (err) => {
let errorMessage = 'Error saving exam';
if (err.error) {
if (typeof err.error === 'string') {
errorMessage = err.error;
} else if (err.error.title) {
errorMessage = err.error.title;
} else if (err.error.message) {
errorMessage = err.error.message;
}
}
alert(errorMessage);
console.error('Error saving exam:', err);
}
});
}
}