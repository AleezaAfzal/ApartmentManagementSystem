
document.addEventListener('DOMContentLoaded', function () {
    const scrollContainer = document.getElementById('scrollContainer');
    const leftArrow = document.getElementById('leftArrow');
    const rightArrow = document.getElementById('rightArrow');

    if (scrollContainer && leftArrow && rightArrow) {
        const cardWidth = 300;
        const gap = 24;
        const scrollAmount = cardWidth + gap;

        const checkArrows = () => {
            const scrollLeft = scrollContainer.scrollLeft;
            const maxScroll = scrollContainer.scrollWidth - scrollContainer.clientWidth;

            if (scrollLeft <= 10) {
                leftArrow.classList.remove('show');
            } else {
                leftArrow.classList.add('show');
            }

            if (scrollLeft >= maxScroll - 10) {
                rightArrow.classList.remove('show');
            } else {
                rightArrow.classList.add('show');
            }
        };

        leftArrow.addEventListener('click', () => {
            scrollContainer.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
            setTimeout(checkArrows, 300);
        });

        rightArrow.addEventListener('click', () => {
            scrollContainer.scrollBy({ left: scrollAmount, behavior: 'smooth' });
            setTimeout(checkArrows, 300);
        });

        scrollContainer.addEventListener('scroll', checkArrows);
        window.addEventListener('resize', checkArrows);
        checkArrows();
    }

    // Reviews Carousel
    const reviewsScrollContainer = document.getElementById('reviewsScrollContainer');
    const reviewsLeftArrow = document.getElementById('reviewsLeftArrow');
    const reviewsRightArrow = document.getElementById('reviewsRightArrow');

    if (reviewsScrollContainer && reviewsLeftArrow && reviewsRightArrow) {
        const reviewCardWidth = 350;
        const reviewGap = 24;
        const reviewScrollAmount = reviewCardWidth + reviewGap;

        const checkReviewArrows = () => {
            const scrollLeft = reviewsScrollContainer.scrollLeft;
            const maxScroll = reviewsScrollContainer.scrollWidth - reviewsScrollContainer.clientWidth;

            if (scrollLeft <= 10) {
                reviewsLeftArrow.classList.remove('show');
            } else {
                reviewsLeftArrow.classList.add('show');
            }

            if (scrollLeft >= maxScroll - 10) {
                reviewsRightArrow.classList.remove('show');
            } else {
                reviewsRightArrow.classList.add('show');
            }
        };

        reviewsLeftArrow.addEventListener('click', () => {
            reviewsScrollContainer.scrollBy({ left: -reviewScrollAmount, behavior: 'smooth' });
            setTimeout(checkReviewArrows, 300);
        });

        reviewsRightArrow.addEventListener('click', () => {
            reviewsScrollContainer.scrollBy({ left: reviewScrollAmount, behavior: 'smooth' });
            setTimeout(checkReviewArrows, 300);
        });

        reviewsScrollContainer.addEventListener('scroll', checkReviewArrows);
        window.addEventListener('resize', checkReviewArrows);
        checkReviewArrows();
    }

    const mobileMenuButton = document.getElementById('mobile-menu-button');
    const mobileMenu = document.getElementById('mobile-menu');

    if (mobileMenuButton && mobileMenu) {
        mobileMenuButton.addEventListener('click', function () {
            mobileMenu.classList.toggle('hidden');
        });
    }

    // Hide navbar when scrolling down from top
    const navbar = document.querySelector('nav');

    if (navbar) {
        window.addEventListener('scroll', function () {
            let scrollTop = window.pageYOffset || document.documentElement.scrollTop;

            if (scrollTop > 100) {
                // Scrolled down - hide navbar
                navbar.style.opacity = '0';
                navbar.style.pointerEvents = 'none';
            } else {
                // At top - show navbar
                navbar.style.opacity = '1';
                navbar.style.pointerEvents = 'auto';
            }
        });
    }
});