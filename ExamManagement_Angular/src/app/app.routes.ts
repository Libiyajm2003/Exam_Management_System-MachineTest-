import { Routes } from '@angular/router';
import { ExamFormComponent } from './components/exam-form/exam-form';
import { ExamListComponent } from './components/exam-list/exam-list';


export const routes: Routes = [
{ path: '', component: ExamFormComponent },
{ path: 'list', component: ExamListComponent },
{ path: '**', redirectTo: '' }
];