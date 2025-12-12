import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ExamMasterService } from '../../services/exam-master';
import { IExamMaster } from '../../models/IExamMaster';


@Component({
selector: 'app-exam-list',
imports: [CommonModule, RouterLink],
templateUrl: './exam-list.html'
})
export class ExamListComponent implements OnInit {
exams: IExamMaster[] = [];
visibleDetails: Set<number> = new Set();

constructor(private examSvc: ExamMasterService) {}


ngOnInit(): void { 
this.load(); 
}


load() { 
this.examSvc.getAll().subscribe({
next: (x) => {
this.exams = x || [];
},
error: (err) => {
console.error('Error loading exams:', err);
alert('Error loading exam records');
}
});
}

toggleDetails(masterID: number) {
if (this.visibleDetails.has(masterID)) {
this.visibleDetails.delete(masterID);
} else {
this.visibleDetails.add(masterID);
}
}

getDetailsVisible(masterID: number | undefined): boolean {
if (!masterID) return false;
return this.visibleDetails.has(masterID);
}
}