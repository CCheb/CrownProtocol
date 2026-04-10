using Godot;
using System;

public partial class TutorialScript : Control
{
	private VBoxContainer slide1;
	private VBoxContainer slide2;
	private VBoxContainer slide3;
	private VBoxContainer slide4;
	private VBoxContainer slide5;
	private VBoxContainer slide6;
	private VBoxContainer slide7;
	private VBoxContainer slide8;
	private VBoxContainer slide9;

	private TextureRect advancedSlideImage;

	private Button nextButton;
	private Button previousButton;

	private int currentSlide = 1;
	private AudioStreamPlayer funnyAudio;
	public override void _Ready()
	{
		slide1 = GetNode<VBoxContainer>("ColorRect/MarginContainer/MarginContainer/Slide1");
		slide2 = GetNode<VBoxContainer>("ColorRect/MarginContainer/MarginContainer/Slide2");
		slide3 = GetNode<VBoxContainer>("ColorRect/MarginContainer/MarginContainer/Slide3");
		slide4 = GetNode<VBoxContainer>("ColorRect/MarginContainer/MarginContainer/Slide4");
		slide5 = GetNode<VBoxContainer>("ColorRect/MarginContainer/MarginContainer/Slide5");
		slide6 = GetNode<VBoxContainer>("ColorRect/MarginContainer/MarginContainer/Slide6");
		slide7 = GetNode<VBoxContainer>("ColorRect/MarginContainer/MarginContainer/Slide7");
		slide8 = GetNode<VBoxContainer>("ColorRect/MarginContainer/MarginContainer/Slide8");
		slide9 = GetNode<VBoxContainer>("ColorRect/MarginContainer/MarginContainer/Slide9");
		advancedSlideImage = GetNode<TextureRect>("ColorRect/MarginContainer/AdvancedTutorial");
		funnyAudio = GetNode<AudioStreamPlayer>("AudioStreamPlayer");

		ShowSlide(1);

		nextButton = GetNode<Button>("ColorRect/NextButton");

		// make previous button invisible on first slide
		previousButton = GetNode<Button>("ColorRect/PrevButton");
		previousButton.Visible = false;
	}

	private void ShowSlide(int slideNumber)
	{
		slide1.Visible = (slideNumber == 1);
		slide2.Visible = (slideNumber == 2);
		slide3.Visible = (slideNumber == 3);
		slide4.Visible = (slideNumber == 4);
		slide5.Visible = (slideNumber == 5);
		slide6.Visible = (slideNumber == 6);
		slide7.Visible = (slideNumber == 7);
		slide8.Visible = (slideNumber == 8);
		slide9.Visible = (slideNumber == 9);
		advancedSlideImage.Visible = (slideNumber == 10);
	}

	private void OnNextPressed()
	{
		if (currentSlide < 10)
		{
			currentSlide++;
			ShowSlide(currentSlide);
		}

		if (currentSlide > 1)
		{
			previousButton.Visible = true;
		}

		if (currentSlide == 9)
		{
			nextButton.Text = "ADVANCED TUTORIAL";
		}
		else if (currentSlide == 10)
		{
			funnyAudio.Play();
			nextButton.Visible = false;
		}
		else
		{
			nextButton.Text = "NEXT";
		}
	}

	private void OnPreviousPressed()
	{
		if (currentSlide > 1)
		{
			currentSlide--;
			ShowSlide(currentSlide);
		}
		if (currentSlide < 10)
		{
			nextButton.Visible = true;
		}
		if (currentSlide == 1)
		{
			previousButton.Visible = false;
		}
	}

	private void OnBackPressed()
	{
		this.Visible = false;
		// set currentSlide to 1 for next time tutorial is opened
		currentSlide = 1;
		ShowSlide(currentSlide);
		nextButton.Text = "NEXT";
		nextButton.Visible = true;
		previousButton.Visible = false;
	}
}
