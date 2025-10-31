import { Component } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { HttpClient, HttpClientModule } from '@angular/common/http';


@Component({
  selector: 'app-forms',
  standalone: true,
  templateUrl: './forms.html',
  styleUrl: './forms.css',
  imports: [ReactiveFormsModule, HttpClientModule],
})
export class Forms {
  number1: FormControl = new FormControl('');
  number2: FormControl = new FormControl('');

  result: FormControl = new FormControl('');

  constructor(private http: HttpClient) { };

  async updateResult() {
    let number1 = parseInt(this.number1.getRawValue() || 0);
    let number2 = parseInt(this.number2.getRawValue() || 0);
    let id = '';

    const payload = { n1: number1, n2: number2 };

    try {
      const postResponse = await this.http
        .post<{ messageId: string }>('http://localhost:1337/CalcAsync', payload)
        .toPromise();

      const id = postResponse?.messageId;

      const getResponse = await this.http
        .get(`http://localhost:1337/GetResult/${id}`)
        .toPromise();

      console.log(`GET response: ${getResponse}`);
      this.result.setValue(getResponse);

    } catch (err) {
      console.log(`error: ${err}`);
    }
  }
}
